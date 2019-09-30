using Common.Log;
using Core.KeyValue;
using Core.User;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class ApiSettingsController : Controller
    {
        private const string _azureTableStorageMetadata = "AzureTableStorage";

        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly ILog _log;
        
        public ApiSettingsController(ILogFactory logFactory, IKeyValuesRepository keyValuesRepository ) : base()
        {
            _log = logFactory.CreateLog(this);
            _keyValuesRepository = keyValuesRepository;
        }

        
        [HttpGet("AzureTableList")]

        //[HttpGet]
        //[SwaggerOperation("ApiSettings")]
        public async Task<IActionResult> Get()
        {   
            try
            {
                var keyValues = await _keyValuesRepository.GetAsync(x =>  x.Types != null && x.Types.Contains(_azureTableStorageMetadata));

                var tableConnStrList = keyValues.Select(x => x.Value).Distinct();

                return new JsonResult(tableConnStrList);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return null;
            }
        }
    }
}