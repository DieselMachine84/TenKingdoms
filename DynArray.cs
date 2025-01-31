using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public abstract class DynArray<T> : IEnumerable<T> where T : class
{
    protected int nextRecNo = 1;
    private List<T> list = new List<T>();
    private Dictionary<int, int> recNoIndexes = new Dictionary<int, int>();

    protected abstract T CreateNewObject(int objectId);
    protected T CreateNew(int objectId = 0)
    {
        T result = CreateNewObject(objectId);

        int unusedIndex = -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                unusedIndex = i;
                break;
            }
        }

        int resultIndex;
        if (unusedIndex != -1)
        {
            list[unusedIndex] = result;
            resultIndex = unusedIndex;
        }
        else
        {
            list.Add(result);
            resultIndex = list.Count - 1;
        }
        
        recNoIndexes.Add(nextRecNo, resultIndex);
        
        return result;
    }

    protected void Delete(int recNo)
    {
        if (!recNoIndexes.ContainsKey(recNo))
            return;

        int index = recNoIndexes[recNo];
        list[index] = default(T);
        recNoIndexes.Remove(recNo);
    }

    public virtual T this[int recNo] => recNoIndexes.ContainsKey(recNo) ? list[recNoIndexes[recNo]] : default(T);

    public virtual bool IsDeleted(int recNo)
    {
        return !recNoIndexes.ContainsKey(recNo);
    }

    public IEnumerator<T> GetEnumerator()
    {
        List<int> keys = recNoIndexes.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            if (!IsDeleted(keys[i]))
            {
                yield return list[recNoIndexes[keys[i]]];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<T> EnumerateRandom()
    {
        List<int> keys = recNoIndexes.Keys.ToList();
        int keyIndex = Misc.Random(keys.Count);
        for (int i = 0; i < keys.Count; i++)
        {
            yield return list[recNoIndexes[keys[keyIndex]]];

            keyIndex++;
            if (keyIndex == keys.Count)
                keyIndex = 0;
        }
    }

    public IEnumerable<int> EnumerateAll(int startRecNo, bool forward)
    {
        List<int> keys = recNoIndexes.Keys.ToList();
        
        int startIndex = -1;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == startRecNo)
            {
                startIndex = i;
            }
        }
        
        if (startIndex == -1)
            yield break;

        int index = startIndex;
        for (int i = 0; i < keys.Count; i++)
        {
            if (forward)
            {
                if (index == keys.Count)
                    index = 0;
            }
            else
            {
                if (index == -1)
                    index = keys.Count - 1;
            }

            yield return keys[index];

            if (forward)
                index++;
            else
                index--;
        }
    }
}