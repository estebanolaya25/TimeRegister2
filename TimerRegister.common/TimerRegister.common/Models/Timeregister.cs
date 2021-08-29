using System;
using System.Collections.Generic;
using System.Text;

namespace TimerRegister.common.Models
{
    public class Timeregister
    {
        public int EmployeeId { get; set; }
        public string Date { get; set; }
        public int TypeEntry { get; set; }
        public bool Consolidated { get; set; }

    }
}
