using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kiota.Builder.Writers.AL;
public class ALObjectIdProvider
{
    private int? _startRange;
    private Dictionary<string, int> _objectCounter;
    public Dictionary<string, int> ObjectCounter => _objectCounter;
    public ALObjectIdProvider()
    {
        _objectCounter = new Dictionary<string, int>();
    }
    public int StartRange
    {
        get
        {
            if (_startRange == null)
                _startRange = 50000;
            return (int)_startRange;
        }
    }
    public int HighestObjectCounter
    {
        get => _objectCounter.Values.Max();
    }
    private void IncrementObjectCounter(string key)
    {
        if (_objectCounter.TryGetValue(key, out var count))
            _objectCounter[key] = count + 1;
        else
            _objectCounter[key] = 1;
    }
    private int GetObjectCounter(string key)
    {
        return _objectCounter.TryGetValue(key, out var count) ? count : 0;
    }
    internal int GetNextObjectId(string key, int startRange = 50000, bool increment = true)
    {
        if (increment)
            IncrementObjectCounter(key);
        if (_startRange == null)
            _startRange = startRange;
        return GetObjectCounter(key) + (int)_startRange;
    }
    internal int GetNextCodeunitId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("codeunit", startRange, increment);
    }
    internal int GetNextPageId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("page", startRange, increment);
    }
    internal int GetNextTableId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("table", startRange, increment);
    }
    internal int GetNextReportId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("report", startRange, increment);
    }
    internal int GetNextXmlPortId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("xmlport", startRange, increment);
    }
    internal int GetNextQueryId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("query", startRange, increment);
    }
    internal int GetNextEnumId(int startRange = 50000, bool increment = true)
    {
        return GetNextObjectId("enum", startRange, increment);
    }
}
