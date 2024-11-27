using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderTrack.Models
{
    public class UpdateModel
    {
        public eStatus Status { get; set; }
        public int Id { get; set; }

        public UpdateModel(eStatus status, int id)
        {
            Status = status;
            Id = id;
        }
    }
}
