using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public interface IIdObject
{
    void SetId(int id);
}

public abstract class DynArray<T> : IEnumerable<T> where T : class, IIdObject
{
    private int _nextId = 1;
    private readonly List<T> _list = new List<T>();
    private readonly Dictionary<int, int> _idIndexes = new Dictionary<int, int>();
    private readonly List<int> _keysForEnumeration = new List<int>();

    protected abstract T CreateNewObject(int objectType);

    protected T CreateNew(int objectType = 0)
    {
        T result = CreateNewObject(objectType);
        result.SetId(_nextId);

        int unusedIndex = -1;
        for (int i = 0; i < _list.Count; i++)
        {
            if (_list[i] == default(T))
            {
                unusedIndex = i;
                break;
            }
        }

        int resultIndex;
        if (unusedIndex != -1)
        {
            _list[unusedIndex] = result;
            resultIndex = unusedIndex;
        }
        else
        {
            _list.Add(result);
            resultIndex = _list.Count - 1;
        }
        
        _idIndexes.Add(_nextId, resultIndex);
        _nextId = GetNextId();
        
        return result;
    }

    protected virtual int GetNextId()
    {
        return _nextId + 1;
    }

    protected void Delete(int id)
    {
        if (!_idIndexes.ContainsKey(id))
            return;

        int index = _idIndexes[id];
        _list[index] = default(T);
        _idIndexes.Remove(id);
    }

    public virtual T this[int id] => _idIndexes.ContainsKey(id) ? _list[_idIndexes[id]] : default(T);

    public virtual bool IsDeleted(int id)
    {
        return !_idIndexes.ContainsKey(id);
    }

    public IEnumerator<T> GetEnumerator()
    {
        List<int> keys = _idIndexes.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            if (!IsDeleted(keys[i]))
            {
                yield return _list[_idIndexes[keys[i]]];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected IEnumerable<T> EnumerateWithDeleted()
    {
        _keysForEnumeration.Clear();
        _keysForEnumeration.AddRange(_idIndexes.Keys);
        
        for (int i = 0; i < _keysForEnumeration.Count; i++)
        {
            if (_idIndexes.ContainsKey(_keysForEnumeration[i]))
            {
                yield return _list[_idIndexes[_keysForEnumeration[i]]];
            }
        }
    }

    public IEnumerable<T> EnumerateRandom()
    {
        _keysForEnumeration.Clear();
        _keysForEnumeration.AddRange(_idIndexes.Keys);
        
        int keyIndex = Misc.Random(_keysForEnumeration.Count);
        for (int i = 0; i < _keysForEnumeration.Count; i++)
        {
            yield return _list[_idIndexes[_keysForEnumeration[keyIndex]]];

            keyIndex++;
            if (keyIndex == _keysForEnumeration.Count)
                keyIndex = 0;
        }
    }

    protected IEnumerable<int> EnumerateAll(int startId, bool forward)
    {
        _keysForEnumeration.Clear();
        _keysForEnumeration.AddRange(_idIndexes.Keys);
        
        int startIndex = -1;
        for (int i = 0; i < _keysForEnumeration.Count; i++)
        {
            if (_keysForEnumeration[i] == startId)
            {
                startIndex = i;
                break;
            }
        }
        
        if (startIndex == -1)
            yield break;

        int index = startIndex;
        for (int i = 0; i < _keysForEnumeration.Count; i++)
        {
            if (forward)
            {
                index++;
                if (index == _keysForEnumeration.Count)
                    index = 0;
            }
            else
            {
                index--;
                if (index == -1)
                    index = _keysForEnumeration.Count - 1;
            }

            yield return _keysForEnumeration[index];
        }
    }
}