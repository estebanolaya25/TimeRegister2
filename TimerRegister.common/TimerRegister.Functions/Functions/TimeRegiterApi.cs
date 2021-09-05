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
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;

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
            else
            {
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

        [FunctionName(nameof(GetAllRegistedTime))]
        public static async Task<IActionResult> GetAllRegistedTime(
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


        [FunctionName(nameof(GetConsolidate))]
        public static async Task<IActionResult> GetConsolidate(
 [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeregister/Consolidate")] HttpRequest req,
 [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable,
 [Table("ConsolidateRegister", Connection = "AzureWebJobsStorage")] CloudTable consolidateRegisterTable,
    ILogger log)
        {
            log.LogInformation("Get all todos received.");

            TableQuery<TimeregisterEntity> query = new TableQuery<TimeregisterEntity>();
            TableQuerySegment<TimeregisterEntity> timeregister = await timeregisterTable.ExecuteQuerySegmentedAsync(query, null);

            List<TimeregisterEntity> listTimeregister = new List<TimeregisterEntity>();
            foreach (TimeregisterEntity lst in timeregister)
            {
                if (!lst.Consolidated)
                {
                    TimeregisterEntity objtimer = new TimeregisterEntity();
                    objtimer.EmployeeId = lst.EmployeeId;
                    objtimer.Date = lst.Date;
                    objtimer.TypeEntry = lst.TypeEntry;
                    objtimer.RowKey = lst.RowKey;
                    objtimer.PartitionKey = lst.PartitionKey;
                    objtimer.ETag = lst.ETag;
                    objtimer.Timestamp = lst.Timestamp;
                    listTimeregister.Add(objtimer);
                }
            }

            listTimeregister = listTimeregister.OrderBy(register => register.EmployeeId).ThenBy(register => register.Timestamp).ToList();

            /*
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
            }else {
                string message = $"There is not any Register for EmployeeId : {date} ";
                log.LogInformation(message);
                return new OkObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = message,
                    Result = listTimeregister
                });

            }*/

            List<Consolidated> ConsolidateTime = new List<Consolidated>();

            DateTime tiempoingreso = DateTime.MaxValue;
            DateTime tiemposalida = DateTime.MaxValue;
            int posicionj = 0;
            int posicioni = 0;

            for (int i = 0; i < listTimeregister.Count; i++)
            {
                if ((listTimeregister[i].TypeEntry == 0) && (!listTimeregister[i].Consolidated))
                {
                    tiempoingreso = Convert.ToDateTime(listTimeregister[i].Date);
                    posicioni = i;

                }
                else if ((listTimeregister[i].TypeEntry == 1) && (!listTimeregister[i].Consolidated))
                {
                    tiemposalida = Convert.ToDateTime(listTimeregister[i].Date);
                    posicionj = i;

                }

                if (!listTimeregister[i].Consolidated)
                {
                    for (int j = 0; j < listTimeregister.Count; j++)
                    {

                        if (j != i)
                        {
                            if (listTimeregister[i].EmployeeId == listTimeregister[j].EmployeeId)
                            {                                
                                if ((listTimeregister[j].TypeEntry == 1) && (!listTimeregister[j].Consolidated))
                                {

                                    DateTime temporal = Convert.ToDateTime(listTimeregister[j].Date);
                                    int result = DateTime.Compare(temporal, tiemposalida);
                                    if (result<0)
                                    { 
                                            tiemposalida = temporal;
                                            posicionj = j;  
                                    }

                                }
                                else if ((listTimeregister[j].TypeEntry == 0) && (!listTimeregister[j].Consolidated))
                                {
                                    DateTime temporal = Convert.ToDateTime(listTimeregister[j].Date);
                                    int result = DateTime.Compare(temporal, tiempoingreso);

                                    if (result < 0)
                                    {
                                       
                                        tiempoingreso = temporal;
                                        posicioni = j;

                                    }

                                }

                            }
                        }
                    }
                }

               if (tiemposalida != DateTime.MaxValue)
                {
                    if (tiempoingreso != DateTime.MaxValue)
                    {
                        Consolidated objconsilidate = new Consolidated();
                        TimeSpan minutos = tiemposalida - tiempoingreso;
                        double intmunitos = minutos.TotalMinutes;
                        listTimeregister[posicioni].Consolidated = true;
                        listTimeregister[posicionj].Consolidated = true;
                        TableOperation findOperationi = TableOperation.Retrieve<TimeregisterEntity>("TimeRegister", listTimeregister[posicioni].RowKey);
                        TableResult findResulti = await timeregisterTable.ExecuteAsync(findOperationi);
                        TimeregisterEntity timeregisterEntityi = (TimeregisterEntity)findResulti.Result;
                        timeregisterEntityi.Consolidated = true;
                        TableOperation addOperationi = TableOperation.Replace(timeregisterEntityi);
                        await timeregisterTable.ExecuteAsync(addOperationi);

                        TableOperation findOperationj = TableOperation.Retrieve<TimeregisterEntity>("TimeRegister", listTimeregister[posicionj].RowKey);
                        TableResult findResultj = await timeregisterTable.ExecuteAsync(findOperationj);
                        TimeregisterEntity timeregisterEntityj = (TimeregisterEntity)findResultj.Result;
                        timeregisterEntityj.Consolidated = true;
                        TableOperation addOperationj = TableOperation.Replace(timeregisterEntityj);
                        await timeregisterTable.ExecuteAsync(addOperationj);



                        objconsilidate.EmployeeId = listTimeregister[posicioni].EmployeeId;
                        //objconsilidate.Date =listTimeregister[posicioni].Date;
                        DateTime aux = Convert.ToDateTime(listTimeregister[posicioni].Date);
                        string straux = aux.ToString("yyyy-MM-dd");
                        objconsilidate.Date = straux;
                        objconsilidate.Minutes = intmunitos;
                        bool has = ConsolidateTime.Any(x => (x.EmployeeId == objconsilidate.EmployeeId) && (x.Date == objconsilidate.Date));
                        if (has)
                        {
                            for (int h = 0; h < ConsolidateTime.Count; h++)
                            {
                                if (ConsolidateTime[h].EmployeeId == objconsilidate.EmployeeId)
                                {
                                    ConsolidateTime[h].Minutes = ConsolidateTime[h].Minutes + objconsilidate.Minutes;
                                }

                            }
                            tiempoingreso = DateTime.MaxValue;
                            tiemposalida = DateTime.MaxValue;

                        }
                        else
                        {
                            ConsolidateTime.Add(objconsilidate);
                            tiempoingreso = DateTime.MaxValue;
                            tiemposalida = DateTime.MaxValue;
                        }

                    }
                }
            }
            
            foreach (Consolidated consolidated in ConsolidateTime)
            {
                ConsolidatedEntity objConsolidate = new ConsolidatedEntity {
                EmployeeId = consolidated.EmployeeId,
                Date = consolidated.Date,
                Minutes = consolidated.Minutes,
                ETag = "*",
                PartitionKey = "ConsolidateRegister",
                RowKey = Guid.NewGuid().ToString()
            };

                TableOperation addOperation = TableOperation.Insert(objConsolidate);
                await consolidateRegisterTable.ExecuteAsync(addOperation);

            }



            string message = "Retrieved all timeregister";
            log.LogInformation(message);
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = ConsolidateTime
            });


        }


    }
}
