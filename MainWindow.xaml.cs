using Microsoft.Win32;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace ViewerBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "IFC File|*.ifc|IFC XML File|*.ifcxml",
                FilterIndex = 0,
                Title = "Select LOIN IFC File",
            };

            if (dlg.ShowDialog() != true)
                return;

            txtFilePath.Text = dlg.FileName;
            ProcessModel(dlg.FileName);

        }

        private void ProcessModel(string path)
        {
            var sink = new TextBlockSink(msg =>
            {
                txtLog.Dispatcher.InvokeAsync(() =>
                {
                    txtLog.ContentEnd.InsertTextInRun(msg);
                    txtLog.ContentEnd.InsertLineBreak();
                });
            });

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(sink)
                .CreateLogger();

            XbimLogging.LoggerFactory.AddSerilog();
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            var package = Path.ChangeExtension(path, "data.zip");

            using (var model = IfcStore.Open(path))
            using (var cache = model.BeginInverseCaching())
            using (var file = File.Create(package))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                Log.Information("Processing and exporting geometry.");
                ExportGeometry(zip, model);
                Log.Information("Exporting properties.");
                ExportSemantic(zip, model);
            }
            Log.Information($"Finished. Result: {package}");
        }

        private void ExportSemantic(ZipArchive zip, IfcStore model)
        {
            foreach (var item in model.Instances.OfType<IIfcProduct>())
            {
                var path = $"api/{item.EntityLabel}/properties.json";

                var psets = new Dictionary<string, Dictionary<string, string>>();

                var typePsets = item.IsTypedBy.Select(t => t.RelatingType).FirstOrDefault()?
                    .HasPropertySets ??
                    Enumerable.Empty<IIfcPropertySetDefinition>();
                ExtractPsets(psets, typePsets);


                var instancePsets = item.IsDefinedBy
                    .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions);
                // override type properties
                ExtractPsets(psets, typePsets);
                // serialize
                var entry = zip.CreateEntry(path);
                using (var stream = entry.Open())
                {
                    var data = JsonSerializer.Serialize(psets);
                    using (var w = new StreamWriter(stream))
                    {
                        w.WriteLine(data);
                    }
                }
            }
        }

        private void ExtractPsets(Dictionary<string, Dictionary<string, string>> result, IEnumerable<IIfcPropertySetDefinition> psets)
        {
            // TODO: we might want to handle quantity sets
            foreach (var pset in psets.OfType<IIfcPropertySet>())
            {
                if (!result.TryGetValue(pset.Name, out Dictionary<string, string> values))
                {
                    values = new Dictionary<string, string>();
                    result.Add(pset.Name, values);
                }

                // TODO: we might want to handle more complex properties as well
                foreach (var prop in pset.HasProperties.OfType<IIfcPropertySingleValue>())
                {
                    if (!values.TryGetValue(prop.Name, out string value))
                        values.Add(prop.Name, prop.NominalValue?.ToString());
                    else
                        values[prop.Name] = prop.NominalValue?.ToString();
                }
            }
        }

        private void ExportGeometry(ZipArchive zip, IfcStore model)
        {
            var context = new Xbim3DModelContext(model);
            context.CreateContext();

            var entity = zip.CreateEntry("model.wexbim");
            using (var memory = new MemoryStream())
            using (var w = new BinaryWriter(memory))
            using (var wexbim = entity.Open())
            {
                model.SaveAsWexBim(w);
                memory.Seek(0, SeekOrigin.Begin);
                memory.CopyTo(wexbim);
                wexbim.Close();
            }
        }
    }
}
