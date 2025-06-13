using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public enum ReadingStatus
    {
        NotRead = 0,      // No leído
        Reading = 1,      // Se está leyendo
        Finished = 2,     // Terminado
        NotFinished = 3   // No terminado
    }
}

