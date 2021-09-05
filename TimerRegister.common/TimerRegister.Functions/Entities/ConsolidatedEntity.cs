using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace TimerRegister.Functions.Entities
{
    public class ConsolidatedEntity : TableEntity
    {
        public int EmployeeId { get; set; }
        public string Date { get; set; }
        public double Minutes { get; set; }
    }
}
