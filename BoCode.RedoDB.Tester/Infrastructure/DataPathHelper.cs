using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoCode.RedoDB.Tester.Infrastructure
{
    public static class DataPathHelper
    {

        public static bool HasFile(this string dataPath, string fileName)
        {
            return Directory.GetFiles(dataPath)
                .Select(x => new FileInfo(x).Name)
                .SingleOrDefault(n => n == fileName) is null ? false: true ;
        }
    }
}
