using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UDMRequestClass;

namespace WebApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        IRMQService RMQService { get; set; }

        public UserController(IRMQService _rmqService)
        {
            RMQService = _rmqService;
        }

        [HttpGet("/api/UDM/user")]
        public async Task<JsonResult> GetUserInfo(string email = "")
        {
            UDMRequest request = new UDMRequest() { Operation = UDMOperation.Read, RequestType = UDMRequestType.User, Email = email };

            try
            {
                string result = RMQService.ExecuteRequest(request, "", "", "rpc_queue");
                UDMUser info = JsonConvert.DeserializeObject<UDMUser>(result, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, PreserveReferencesHandling = PreserveReferencesHandling.All });
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new JsonResult(null);
            }
        }

        [HttpGet("/api/UDM/portfolio")]
        public async Task<JsonResult> GetPortfolioInfo(string email = "")
        {
            UDMRequest request = new UDMRequest() { Operation = UDMOperation.Read, RequestType = UDMRequestType.Portfolio, Email = null };

            try
            {
                string result = RMQService.ExecuteRequest(request, "", "", "rpc_queue");
                List<UDMPortfolio> info = JsonConvert.DeserializeObject<List<UDMPortfolio>>(result, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, PreserveReferencesHandling = PreserveReferencesHandling.All });
                return new JsonResult(info);
            }
            catch (Exception ex)
            {
                return new JsonResult(null);
            }
        }

        [HttpGet("/api/UDM/buyportfolio")]
        public async Task<JsonResult> BuyPortfolio(UDMPortfolio portfolio)
        {
            //UDMRequest request = new UDMRequest() { Operation = UDMOperation.Read, RequestType = UDMRequestType.User, Email = email };
            try
            {
                Console.WriteLine("PURCHASES");
                //string result = RMQService.ExecuteRequest(request, "", "", "rpc_queue");
                //UDMUser info = JsonConvert.DeserializeObject<UDMUser>(result, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, PreserveReferencesHandling = PreserveReferencesHandling.All });
                return new JsonResult(null);
            }
            catch (Exception ex)
            {
                return new JsonResult(null);
            }
        }

        [HttpPost("/api/UDM/newuser")]
        public IActionResult PostUserDataForm(UDMUser userDataForm)
        {
            UDMUser User = new UDMUser
            {
                Email = userDataForm.Email,
                FirstName = userDataForm.FirstName,
                LastName = userDataForm.LastName,
                BrokerageAccount = userDataForm.BrokerageAccount
            };

            UDMRequest request = new UDMRequest
            {
                RequestType = UDMRequestType.User,
                Operation = UDMOperation.Insert,
                User = User
            };

            try
            {
                string result = RMQService.ExecuteRequest(request, "", "", "rpc_queue");
                UDMUser info = JsonConvert.DeserializeObject<UDMUser>(result, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, PreserveReferencesHandling = PreserveReferencesHandling.All });
                return new JsonResult(info);
            }
            catch (Exception ex)
            {
                return new JsonResult(null);
            }
        }
    }
}