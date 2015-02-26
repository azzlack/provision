namespace Provision.Tests.Extensions
{
    using Newtonsoft.Json;

    public static class ObjectExtensions
    {
        /// <summary>
        /// Clones the specified source.
        /// </summary>
        /// <typeparam name="T">The source type.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns>A clone of the specified source that occupies a different memory slot.</returns>
        public static T Clone<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }
    }
}