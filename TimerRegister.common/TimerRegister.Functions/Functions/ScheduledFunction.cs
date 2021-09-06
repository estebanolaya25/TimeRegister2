using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using TimerRegister.common.Models;
using TimerRegister.common.Responses;
using TimerRegister.Functions.Entities;

namespace TimerRegister.Functions.Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static async Task Run([TimerTrigger("*/10 * * * *")] TimerInfo myTimer,
        [Table("timeregister", Connection = "AzureWebJobsStorage")] CloudTable timeregisterTable,
        [Table("ConsolidateRegister", Connection = "AzureWebJobsStorage")] CloudTable consolidateRegisterTable,
            ILogger log)
        {
            log.LogInformation("consolidation process begins!.");

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
                                    if (result < 0)
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
                ConsolidatedEntity objConsolidate = new ConsolidatedEntity
                {
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





            log.LogInformation($"Consolidation procedure run at: {DateTime.Now}");

        }





    }
}
