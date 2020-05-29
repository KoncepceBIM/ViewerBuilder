using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
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
            // direct log to text block
            var sink = new TextSink(msg =>
            {
                txtLog.Dispatcher.InvokeAsync(() =>
                {
                    txtLog.ContentEnd.InsertTextInRun(msg);
                    txtLog.ContentEnd.InsertLineBreak();
                });
            });

            // set up logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(sink)
                .CreateLogger();

            // set up xbim logging
            XbimLogging.LoggerFactory.AddSerilog();
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            var package = Path.ChangeExtension(path, "web.zip");

            Log.Information($"Opening model: {path}");
            
            // open model and extract all bits and pieces
            using (var model = IfcStore.Open(path))
            using (var cache = model.BeginInverseCaching())
            using (var file = File.Create(package))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                Log.Information("Inserting static application files...");
                InsertStaticApp(zip);

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
                ExtractPsets(psets, instancePsets);
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
            // TODO: we might want to handle quantity sets as well
            foreach (var pset in psets.OfType<IIfcPropertySet>().Where(ps => 
                ps.Name.ToString().StartsWith("cz_", StringComparison.OrdinalIgnoreCase) ||
                ps.Name.ToString().StartsWith("pset_", StringComparison.OrdinalIgnoreCase)
                ))
            {
                if (!result.TryGetValue(pset.Name, out Dictionary<string, string> values))
                {
                    values = new Dictionary<string, string>();
                    result.Add(pset.Name, values);
                }

                foreach (var prop in pset.HasProperties.OfType<IIfcPropertySingleValue>())
                {
                    if (!values.TryGetValue(prop.Name, out string value))
                        values.Add(prop.Name, prop.NominalValue?.ToString());
                    else
                        values[prop.Name] = prop.NominalValue?.ToString();
                }

                foreach (var prop in pset.HasProperties.OfType<IIfcPropertyEnumeratedValue>())
                {
                    var value = string.Join(", ", prop.EnumerationValues.Select(v => v.ToString()));
                    if (!values.TryGetValue(prop.Name, out string _))
                        values.Add(prop.Name, value);
                    else
                        values[prop.Name] = value;
                }

                // TODO: we might want to handle more complex properties as well (complex properties, reference properties, tables, ...)
            }
        }

        private void ExportGeometry(ZipArchive zip, IfcStore model)
        {
            var context = new Xbim3DModelContext(model);
            context.CreateContext();

            var entity = zip.CreateEntry("api/model.wexbim");
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

        private void InsertStaticApp(ZipArchive archive)
        {
            var uri = new Uri("/app/dist/dist.zip", UriKind.Relative);
            var info = Application.GetResourceStream(uri);
            var appStream = info.Stream;
            using (var appArchive = new ZipArchive(appStream, ZipArchiveMode.Read))
            {
                foreach (var appEntry in appArchive.Entries)
                {
                    var copyEntry = archive.CreateEntry(appEntry.FullName);
                    using (var copyStream = copyEntry.Open())
                    using (var item = appEntry.Open())
                    {
                        item.CopyTo(copyStream);
                    }
                } 
            }
        }
    }
}
