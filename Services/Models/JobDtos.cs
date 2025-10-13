using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models
{
    public record JobDtos(Guid Id, int Kind, int Status, string InputPath, DateTime CreatedAt, DateTime? CompletedAt);
}
