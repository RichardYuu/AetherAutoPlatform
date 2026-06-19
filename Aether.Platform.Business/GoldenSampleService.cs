using System;
using System.Collections.Generic;
using Aether.Platform.Core.Interfaces;

namespace Aether.Platform.Business
{
    public class GoldenSampleService : IGoldenSampleService
    {
        public void CreateSample(string partNumber, string sampleName, string qrCode, Dictionary<string, object> testData) { }
        public void AutoSample(string partNumber, int testCount, bool useAverage) { }
        public bool ValidateSample(string partNumber, Dictionary<string, object> testData, double tolerance) => true;
        public IReadOnlyList<object> GetSampleHistory(string partNumber) => new List<object>();
    }
}
