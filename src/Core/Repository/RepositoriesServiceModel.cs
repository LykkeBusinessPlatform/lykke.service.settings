using System.Collections.Generic;
using Core.Extensions;

namespace Core.Repository
{
    public class RepositoriesServiceModel
    {
        /// <summary>
        /// status code
        /// </summary>
        public UpdateSettingsStatus Result { get; set; }

        /// <summary>
        /// message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// json data
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        /// additional data
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// additional ienumerable data
        /// </summary>
        public IEnumerable<object> CollectionData { get; set; }
    }
}
