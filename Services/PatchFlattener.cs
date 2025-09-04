using System.Text.Json;
namespace IDV_Templates_Mongo_API.Services;
internal static class PatchFlattener
{
    public static Dictionary<string, object> Flatten(object? value, string prefix = "")
    {
        var result = new Dictionary<string, object>();
        if (value is null) return result;
        object? val = value;
        if (val is JsonElement je) { val = je.Deserialize<object>(); }
        if (val is IDictionary<string, object> dict)
        {
            foreach (var kv in dict)
            {
                var key = string.IsNullOrEmpty(prefix) ? kv.Key : $"{prefix}.{kv.Key}";
                foreach (var nested in Flatten(kv.Value, key)) result[nested.Key] = nested.Value;
            }
        }
        else if (val is System.Text.Json.Nodes.JsonObject jObj)
        {
            foreach (var kv in jObj)
            {
                var key = string.IsNullOrEmpty(prefix) ? kv.Key : $"{prefix}.{kv.Key}";
                foreach (var nested in Flatten(kv.Value, key)) result[nested.Key] = nested.Value;
            }
        }
        else if (val is IEnumerable<object> arr && !(val is string))
        {
            int i = 0;
            foreach (var item in arr)
            {
                var key = string.IsNullOrEmpty(prefix) ? $"{i}" : $"{prefix}.{i}";
                foreach (var nested in Flatten(item, key)) result[nested.Key] = nested.Value;
                i++;
            }
        }
        else { result[prefix] = val!; }
        return result;
    }
}
