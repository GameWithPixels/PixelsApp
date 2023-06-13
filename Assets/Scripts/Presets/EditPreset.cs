// Ignore Spelling: serializer

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Presets
{
    [System.Serializable]
    public class EditDieAssignment
        : EditObject
    {
        public EditDie die;
        public EditProfile behavior;
    }

    class EditDieAssignmentConverter
        : JsonConverter<EditDieAssignment>
    {
        AppDataSet dataSet;
        public EditDieAssignmentConverter(AppDataSet dataSet)
        {
            this.dataSet = dataSet;
        }

        public override void WriteJson(JsonWriter writer, EditDieAssignment value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("systemId");
            if (value.die != null)
                serializer.Serialize(writer, value.die.systemId);
            else
                serializer.Serialize(writer, (ulong)0);
            writer.WritePropertyName("behaviorIndex");
            serializer.Serialize(writer, dataSet.profiles.IndexOf(value.behavior));
            writer.WriteEndObject();
        }

        public override EditDieAssignment ReadJson(JsonReader reader, System.Type objectType, EditDieAssignment existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (hasExistingValue)
                throw new System.NotImplementedException();

            var ret = new EditDieAssignment();    
            JObject jsonObject = JObject.Load(reader);
            var systemId = jsonObject["systemId"]?.ToObject<string>();
            ret.die = dataSet.GetEditDie(systemId);
            int behaviorIndex = jsonObject["behaviorIndex"].Value<int>();
            if (behaviorIndex >= 0 && behaviorIndex < dataSet.profiles.Count)
                ret.behavior = dataSet.profiles[behaviorIndex];
            else
                ret.behavior = null;
            return ret;
        }
    }

    [System.Serializable]
    public class EditPreset
        : EditObject
    {
        public string name;
        public string description;
        public List<EditDieAssignment> dieAssignments = new List<EditDieAssignment>();

        public bool CheckDependency(EditDie die)
        {
            return dieAssignments.Any(ass => ass.die == die);
        }

        public EditPreset Duplicate()
        {
            return new EditPreset
            {
                name = name,
                description = description,
                dieAssignments = dieAssignments.Select(a => new EditDieAssignment { die = a.die, behavior = a.behavior }).ToList(),
            };
        }

        public void DeleteProfile(EditProfile profile)
        {
            foreach (var ass in dieAssignments)
            {
                if (ass.behavior == profile)
                {
                    ass.behavior = null;
                }
            }
        }

        public bool DependsOnProfile(EditProfile profile)
        {
            return dieAssignments.Any(d => d.behavior == profile);
        }

        public void DeleteDie(EditDie die)
        {
            foreach (var ass in dieAssignments)
            {
                if (ass.die == die)
                {
                    ass.die = null;
                }
            }
        }

        public bool DependsOnDie(EditDie die)
        {
            return dieAssignments.Any(d => d.die == die);
        }

        public bool IsActive()
        {
            bool ret = true;
            foreach (var assignment in dieAssignments)
            {
                if (assignment.die == null)
                {
                    ret = false;
                }
                else if (assignment.die.currentBehavior != assignment.behavior)
                {
                    ret = false;
                    break;
                }
            }
            return ret;
        }
    }
}

