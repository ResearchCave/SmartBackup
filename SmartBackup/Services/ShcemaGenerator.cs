using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using SmartBackup.Model;
using Newtonsoft.Json;
using NJsonSchema.Generation;
using Serilog;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Namotion.Reflection;

namespace SmartBackup.Services
{
    public class SchemaGenerator
    {
        readonly PluginManager PluginMgr;
        readonly ILogger log;
        public SchemaGenerator(PluginManager _PluginMgr, ILogger _log)
        {

            log = _log;
            PluginMgr = _PluginMgr;
        }
        public string GenerateSchemaForClass(Type myType)
        {
            JSchemaGenerator jsonSchemaGenerator = new JSchemaGenerator();
            //   jsonSchemaGenerator.DefaultRequired = Newtonsoft.Json.Required.Default;
            jsonSchemaGenerator.DefaultRequired = Required.DisallowNull;
            jsonSchemaGenerator.SchemaReferenceHandling = SchemaReferenceHandling.Objects;
            //jsonSchemaGenerator.SchemaReferenceHandling = SchemaReferenceHandling.Objects;
            StringEnumGenerationProvider segp = new StringEnumGenerationProvider();
            segp.CamelCaseText = false;
            // jsonSchemaGenerator.SchemaIdGenerationHandling = SchemaIdGenerationHandling.TypeName;
            jsonSchemaGenerator.GenerationProviders.Add(segp);
            JSchema schema = jsonSchemaGenerator.Generate(myType, false);

            schema.Title = myType.Name;
            // schema.AllowAdditionalProperties = false;
            schema.Properties["Items"].UniqueItems = true;

            string[] modulenames = PluginMgr.Modules.Keys.ToArray();

            schema.Properties["Items"].Type = JSchemaType.Array;
            schema.Properties["Items"].ItemsPositionValidation = false;

            //   schema.Properties["Items"].Properties["Name"].UniqueItems = true;

            foreach (var m in PluginMgr.Modules)
            {
                JSchema ArraySchema = new JSchema();
                ArraySchema.Type = JSchemaType.Array;
                ArraySchema.ItemsPositionValidation = false;

                Type t = m.Value.BackupInfo;
                JSchema subschema = jsonSchemaGenerator.Generate(t, false);
                subschema.AllowAdditionalProperties = false;
                subschema.AllowUnevaluatedItems = false;

                foreach (string mn in modulenames)
                {
                    subschema.Properties["Type"].Enum.Add(mn);
                }

                ArraySchema.Items.Clear();

                JSchema jx = new JSchema();
                jx.If = new JSchema();
                jx.If.Properties.Add("Type", new JSchema());
                jx.If.Properties["Type"].Const = m.Key;
                jx.Then = subschema;

                ArraySchema.Items.Add(jx);

                log.Debug("\tInjectiong Schema for Module {0} Type:{1}", m.Value.BackupModule, t);

                schema.Properties["Items"].AllOf.Add(ArraySchema);

            }
            //  ArraySchema.AllOf.Properties["Name"].UniqueItems = true;

            //     schema.Properties["Items"].Items..Properties["Name"].UniqueItems = true;

            //JSchema gg = new JSchema();
            //JSchema nameschema = new JSchema();
            //nameschema.Type = JSchemaType.String;
            //gg.Properties.Add("Name", nameschema);
            //gg.Properties["Name"].UniqueItems = true;

            //JSchema juniq = new JSchema();
            //JSchema jname = new JSchema();
            //jname.Type = JSchemaType.String;
            //juniq.Properties.Add("Name", jname);
            //juniq.UniqueItems = true;

            //schema.Properties["Items"].Items.Add (juniq);

            return schema.ToString();
        }

    }
}
