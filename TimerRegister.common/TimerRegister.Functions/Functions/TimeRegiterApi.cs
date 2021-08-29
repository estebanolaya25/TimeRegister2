using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using TimerRegister.common.Responses;
using TimerRegister.common.Models;
using TimerRegister.Functions.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TimerRegister.Functions.Functions
{
    public static class TimeRegiterApi
    {
        [FunctionName(nameof(CreateTimeRegister))]
        public static async Task<IActionResult> CreateTimeRegister(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timeregister")] HttpRequest req,
           [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable,
           ILogger log)
        {
            log.LogInformation("Recieved a new Time Register");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Timeregister Timeregister = JsonConvert.DeserializeObject<Timeregister>(requestBody);
            
            if (Timeregister == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have all Items"
                });
            }
            DateTime Now = DateTime.Now;

            log.LogInformation(Now.ToString());

            TimeregisterEntity timeregisterEntity = new TimeregisterEntity
            {
                EmployeeId = Timeregister.EmployeeId,
                Date = Timeregister.Date,
                TypeEntry = Timeregister.TypeEntry,
                Consolidated = false,
                ETag = "*",                               
                PartitionKey = "TimeRegister",
                RowKey = Guid.NewGuid().ToString()
            };

            TableOperation addOperation = TableOperation.Insert(timeregisterEntity);
            await timeregisterTable.ExecuteAsync(addOperation);
            string message = "New Timer Register add";
            log.LogInformation(message);
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeregisterEntity
            });
        }
        [FunctionName(nameof(GetAlltimeregisterByEmployedId))]
        public static async Task<IActionResult> GetAlltimeregisterByEmployedId(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeregister/{IdEmpleado}")] HttpRequest req,
         [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable, int IdEmpleado,
            ILogger log)
        {
            log.LogInformation("Get all todos received.");

            TableQuery<TimeregisterEntity> query = new TableQuery<TimeregisterEntity>();
            TableQuerySegment<TimeregisterEntity> timeregister = await timeregisterTable.ExecuteQuerySegmentedAsync(query, null);

            List<TimeregisterEntity> listTimeregister = new List<TimeregisterEntity>();

            foreach (TimeregisterEntity lst in timeregister)
            {
                if (lst.EmployeeId == IdEmpleado)
                {
                    TimeregisterEntity objtimer = new TimeregisterEntity();
                    objtimer.EmployeeId = lst.EmployeeId;
                    objtimer.Date = lst.Date;
                    objtimer.RowKey = lst.RowKey;
                    objtimer.PartitionKey = lst.PartitionKey;
                    objtimer.ETag = lst.ETag;
                    objtimer.Timestamp = lst.Timestamp;
                    listTimeregister.Add(objtimer);

                }
            }
            if (listTimeregister.Count != 0)
            {
                string message = "Retrieved all timeregister";
                log.LogInformation(message);
                return new OkObjectResult(new Response
                {
                    IsSuccess = true,
                    Message = message,
                    Result = listTimeregister
                });
            }
            else {
                string message = $"There is not any Register for EmployeeId : {IdEmpleado} ";
                log.LogInformation(message);
                return new OkObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = message,
                    Result = listTimeregister
                });

            }


        }

        [FunctionName(nameof(GetAllTodo))]
        public static async Task<IActionResult> GetAllTodo(
   [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeregister")] HttpRequest req,
   [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable,
   ILogger log)
        {
            log.LogInformation("Get all todos received.");

            TableQuery<TimeregisterEntity> query = new TableQuery<TimeregisterEntity>();
            TableQuerySegment<TimeregisterEntity> todos = await timeregisterTable.ExecuteQuerySegmentedAsync(query, null);


            string message = "Retrieved all todos";
            log.LogInformation(message);
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todos
            });
        }

        [FunctionName(nameof(UpdateTimerRegister))]
        public static async Task<IActionResult> UpdateTimerRegister(
   [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "timeregister/{id}")] HttpRequest req,
   [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable,
   string id,
   ILogger log)
        {
            log.LogInformation($"Update for to do :{id}, received.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Timeregister timeregister = JsonConvert.DeserializeObject<Timeregister>(requestBody);

            //Validate Todo id
            TableOperation findOperation = TableOperation.Retrieve<TimeregisterEntity>("TimeRegister", id);
            TableResult findResult = await timeregisterTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Time Register not found."

                });
            }
            // Update todo

            TimeregisterEntity timeregisterEntity = (TimeregisterEntity)findResult.Result;
            timeregisterEntity.Date = timeregister.Date;
            timeregisterEntity.TypeEntry = timeregister.TypeEntry;
            timeregisterEntity.Consolidated = timeregister.Consolidated;

            TableOperation addOperation = TableOperation.Replace(timeregisterEntity);
            await timeregisterTable.ExecuteAsync(addOperation);

            string message = $"Todo: {id}, is Update";
            log.LogInformation(message);
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeregisterEntity
            });
        }



        [FunctionName(nameof(DeleteTimerRegister))]
        public static async Task<IActionResult> DeleteTimerRegister(
   [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "timeregister/{id}")] HttpRequest req,
   [Table("timeregister", "TimeRegister", "{id}", Connection = "AzureWebJobsStorage")] TimeregisterEntity timeregisterEntity,
   [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable todoTable, string id,
   ILogger log)
        {
            log.LogInformation($"Delete todo:{id} received.");

            if (timeregisterEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "todo not found."

                });
            }
            await todoTable.ExecuteAsync(TableOperation.Delete(timeregisterEntity));

            string message = $"Todo: {timeregisterEntity.RowKey}, delete.";
            log.LogInformation(message);
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeregisterEntity
            });
        }


        [FunctionName(nameof(GetAlltimeregisterBydate))]
        public static async Task<IActionResult> GetAlltimeregisterBydate(
 [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeregister/{date}")] HttpRequest req,
 [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable, DateTime date,
    ILogger log)
        {
            log.LogInformation("Get all todos received.");

            TableQuery<TimeregisterEntity> query = new TableQuery<TimeregisterEntity>();
            TableQuerySegment<TimeregisterEntity> timeregister = await timeregisterTable.ExecuteQuerySegmentedAsync(query, null);

            List<TimeregisterEntity> listTimeregister = new List<TimeregisterEntity>();

            foreach (TimeregisterEntity lst in timeregister)
            {
               if (date.Date == Convert.ToDateTime(lst.Date).Date)
                {
                    TimeregisterEntity objtimer = new TimeregisterEntity();
                    objtimer.EmployeeId = lst.EmployeeId;
                    objtimer.Date = lst.Date;
                    objtimer.RowKey = lst.RowKey;
                    objtimer.PartitionKey = lst.PartitionKey;
                    objtimer.ETag = lst.ETag;
                    objtimer.Timestamp = lst.Timestamp;
                    listTimeregister.Add(objtimer);
                }
            }
            if (listTimeregister.Count != 0)
            {
                string message = "Retrieved all timeregister";
                log.LogInformation(message);
                return new OkObjectResult(new Response
                {
                    IsSuccess = true,
                    Message = message,
                    Result = listTimeregister
                });
            }
            else
            {
                string message = $"There is not any Register for EmployeeId : {date} ";
                log.LogInformation(message);
                return new OkObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = message,
                    Result = listTimeregister
                });

            }


        }


    }
}
