// Code generated by Microsoft (R) AutoRest Code Generator 1.0.1.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// AllCollectionTypes
    /// </summary>
    /// <remarks>
    /// AllCollectionTypes
    /// </remarks>
    public partial class AllCollectionTypes
    {
        /// <summary>
        /// Initializes a new instance of the AllCollectionTypes class.
        /// </summary>
        public AllCollectionTypes()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AllCollectionTypes class.
        /// </summary>
        public AllCollectionTypes(IList<int> intArray = default(IList<int>), IList<int> intList = default(IList<int>), IList<string> stringArray = default(IList<string>), IList<string> stringList = default(IList<string>), IList<Poco> pocoArray = default(IList<Poco>), IList<Poco> pocoList = default(IList<Poco>), IDictionary<string, IList<Poco>> pocoLookup = default(IDictionary<string, IList<Poco>>), IDictionary<string, IList<IDictionary<string, Poco>>> pocoLookupMap = default(IDictionary<string, IList<IDictionary<string, Poco>>>))
        {
            IntArray = intArray;
            IntList = intList;
            StringArray = stringArray;
            StringList = stringList;
            PocoArray = pocoArray;
            PocoList = pocoList;
            PocoLookup = pocoLookup;
            PocoLookupMap = pocoLookupMap;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IntArray")]
        public IList<int> IntArray { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IntList")]
        public IList<int> IntList { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "StringArray")]
        public IList<string> StringArray { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "StringList")]
        public IList<string> StringList { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PocoArray")]
        public IList<Poco> PocoArray { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PocoList")]
        public IList<Poco> PocoList { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PocoLookup")]
        public IDictionary<string, IList<Poco>> PocoLookup { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PocoLookupMap")]
        public IDictionary<string, IList<IDictionary<string, Poco>>> PocoLookupMap { get; set; }

    }
}