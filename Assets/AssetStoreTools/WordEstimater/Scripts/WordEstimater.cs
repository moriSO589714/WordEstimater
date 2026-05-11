using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public class WordEstimater
{
    private WordEmtCell _wordsLib;
    private string _separateMark;

    private string _oldInput = "";
    private string[] _oldSplitWords = null;
    private List<WordEmtCell> _oldWordEmtCells = null;
    private List<string> _oldReturnList = null;
    private int _oldDepth;
    public WordEstimater(WordEmtCell wordsLib, string separateMark)
    {
        _wordsLib = wordsLib;
        _separateMark = separateMark;
    }

    private void SetDatas(string oldInput, string[] oldSplitWords, List<WordEmtCell> oldWordEmtCells, List<string> oldReturnList, int oldDepth)
    {
        _oldInput = oldInput;
        _oldSplitWords = oldSplitWords;
        _oldWordEmtCells = oldWordEmtCells;
        _oldReturnList = oldReturnList;
        _oldDepth = oldDepth;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="returnDepth">入力しかけの単語以降、何単語目までの候補を返すか(0なら書きかけの単語部のみ)</param>
    /// <returns></returns>
    public List<string> ReturnEstimatedStrs(string input, int returnDepth)
    {
        string[] splitInput = input.Split(_separateMark);
        //最後が区切り文字で終わっている場合は除去
        if (splitInput.Last() == "")
        {
            List<string> removed = splitInput.ToList();
            removed.RemoveAt(splitInput.Length - 1);
            splitInput = removed.ToArray();
        }

        string[] differenceInput = null;
        int samePoint = -1;
        WordEmtCell searchStartCell = null;
        List<WordEmtCell> recordCells = new List<WordEmtCell>();

        //もし検索が2回目以降であれば、前回との差分を取得して処理を抑える
        if (_oldSplitWords != null && _oldSplitWords.Count() != 0)
        {
            //何文節目まで一致しているか
            samePoint = ReturnSamePoint(splitInput);

            if (samePoint != -1) //増加または同じ単語数
            {
                //前回の文章が完全に新しい入力に含まれている場合(絶対に前回のinputよりも今回のinputのほうが長い)
                //(例) 1: /aaa bbb ccc ddd
                //     2: /aaa bbb ccc ddd eee
                if (input.StartsWith(_oldInput))
                {
                    int oldReturnLength = DepthToOverAll(_oldDepth, _oldSplitWords.Length);
                    int newReturnLength = DepthToOverAll(returnDepth, splitInput.Length);

                    //返す文章のリストが前回とまったく同じと想定される場合
                    if (splitInput.SequenceEqual(_oldSplitWords) && _oldDepth == returnDepth)
                    {
                        return _oldReturnList;
                    }
                    //返す候補文の深さが前回返したもの以下か、無制限
                    else if (oldReturnLength >= newReturnLength || (oldReturnLength == -1 && newReturnLength == -1))
                    {
                        List<string> reuseReturnStr = RemoveFromOldReturn(splitInput, samePoint, newReturnLength);
                        SetDatas(input, splitInput, _oldWordEmtCells, new List<string>(reuseReturnStr), returnDepth);
                        return reuseReturnStr;
                    }
                    //完全一致しているが、前回返した文章以降の要素も求められている場合
                    else if (oldReturnLength < newReturnLength)
                    {
                        List<WordEmtCell> cells = new List<WordEmtCell>();
                        List<string> overAllList = new List<string>();
                        WordEmtCell oldLastCell = _oldWordEmtCells.Last();
                        cells = SearchWords(_oldSplitWords.Last(), oldLastCell);
                        string beforePath = string.Join(_separateMark, _oldSplitWords.Take(_oldSplitWords.Length - 1).ToArray()) + _separateMark;

                        foreach (WordEmtCell wec in cells)
                        {
                            List<string> candidateStrs = new List<string>();
                            ConnectToDeepth(wec, beforePath + wec._myWord, returnDepth, candidateStrs);
                            overAllList.AddRange(candidateStrs);
                        }
                        SetDatas(input, splitInput, _oldWordEmtCells, new List<string>(overAllList), returnDepth);
                        return overAllList;
                    }
                }
            }

            //前回との文章といずれかの場所まで一致する部分が存在する
            //※文章量が前回以上だが、上処理のどの条件にも当てはまらない場合　or　前回よりも文字数が少ない場合
            //(例) 1: /aaa bbb ccc ddd
            //     2: /aaa bbb fff nnn
            //この場合、bbb以降の検索となる
            searchStartCell = RemenberOldParts(splitInput, ref differenceInput, samePoint);
            if (differenceInput == null) throw new Exception("differenceInput is null");
        }
        else //初めての検索
        {
            differenceInput = splitInput;
            searchStartCell = _wordsLib;
        }

        List<WordEmtCell> candidateCells = new List<WordEmtCell>();
        if (differenceInput.Length == 1)
        {
            candidateCells = SearchWords(differenceInput[0], searchStartCell);
        }
        else if (1 < differenceInput.Length)
        {
            WordEmtCell searchedWec = DeepnTree(differenceInput, searchStartCell, recordCells);
            //指定の語が含まれていなかった場合
            if (searchedWec == null) throw new Exception("did not find to specified sentence");
            string lastWord = differenceInput.Last();
            candidateCells = SearchWords(lastWord, searchedWec);
        }
        //一致する候補が見つからない場合
        if (candidateCells.Count == 0) 
        {
            return null;
        }

        //優先度順にソート
        candidateCells = candidateCells.OrderByDescending(x => x._priority).ToList();

        //入力された文章以後の候補も検索しにいく
        List<string> returnList = new List<string>();
        foreach (WordEmtCell wec in candidateCells)
        {
            List<string> candidateStrs = new List<string>();
            string beforePath = "";
            //リストの要素が1つのみであった場合リストを結合させない(_separateMarkのみになるため)
            if(splitInput.Length  != 1)
            {
                beforePath = String.Join(_separateMark, splitInput.Take(splitInput.Length - 1).ToArray()) + _separateMark;
            }

            ConnectToDeepth(wec, beforePath + wec._myWord, returnDepth, candidateStrs);
            returnList.AddRange(candidateStrs);
        }

        SetDatas(input, splitInput, recordCells, returnList, returnDepth);
        return returnList;
    }

    /// <summary>
    /// 入力された文章が何単語目まで前回と一致しているか
    /// </summary>
    /// <returns>1単語も一致しない場合-1を返す</returns>
    private int ReturnSamePoint(string[] splitInput)
    {
        int samePoint = -1;
        //前回の入力よりも今回の入力のほうが短い場合
        if (splitInput.Count() < _oldSplitWords.Count())
        {
            samePoint = IsSameStr(splitInput.Count());
        }
        //前回の入力よりも今回の入力の長さのほうが同じかそれ以上
        else
        {
            samePoint = IsSameStr(_oldSplitWords.Count());
        }
        
        int IsSameStr(int arrayLength)
        {
            int num = -1;
            for(int i = 0; i < arrayLength; i++)
            {
                if (splitInput[i] == _oldSplitWords[i])
                {
                    num = i;
                }
                else
                {
                    return -1;
                }
            }
            return num;
        }

        return samePoint;
    }

    /// <summary>
    /// 差分を取得して前回の途中から検索できるようにする
    /// </summary>
    /// <returns>検索に利用される重複部分削除後の配列</returns>
    private WordEmtCell RemenberOldParts(string[] splitInput, ref string[] differentStrArray, int samePoint)
    {
        //1単語も一致していない場合
        //(例) 1: /aaa bbb ccc
        //     2: /ddd eee fff
        if (samePoint == -1)
        {
            differentStrArray = splitInput;
            return _wordsLib;
        }
        //最後の1単語より前のどこかまで一致している場合
        //(例) 1: /aaa bbb ccc ddd eee
        //     2: /aaa bbb fff ggg hhh
        else
        {
            differentStrArray = new string[(splitInput.Length - 1) - (samePoint + 1) + 1];
            //今回与えられている入力のうち、前回と異なる位置から最後まで引数で渡された配列にコピーする
            Array.Copy(splitInput, samePoint + 1, differentStrArray, 0, splitInput.Length - 1);
        }

        if(differentStrArray == null)
        {
            throw new Exception("differentStrArray is null");
        }
        //前回の検索開始セルは実際の文章のインデックスよりも１つ上に来るため、1を足す
        return _oldWordEmtCells[samePoint + 1];
    }

    /// <summary>
    /// 前回返したstring配列を再利用できる場合に今回条件には合わない文章を排除する
    /// </summary>
    private List<string> RemoveFromOldReturn(string[] input, int samePoint, int maxLength)
    {
        List<string> removedStrArray = new List<string>();
        foreach (string oldReturnStr in _oldReturnList)
        {
            string[] strs = oldReturnStr.Split(_separateMark);
            List<string> trimStr = new List<string>();
            bool passFlag = false;
            //予測候補に新しい文章分の単語数が無かった場合は追加しない
            if (strs.Count() < input.Count()) continue;

            //samePointまでの分節を入れておく
            for (int i = 0; i <= samePoint; i++)
            {
                trimStr.Add(strs[i]);
            }

            for (int i = samePoint + 1; i < strs.Length; i++)
            {
                //最後の入力のみは書きかけでも含めるようにする
                if (i == input.Length - 1)
                {
                    if (!strs[i].StartsWith(input[i]))
                    {
                        passFlag = true;
                    }
                    trimStr.Add(strs[i]);
                    break;
                }
                else if (strs[i] != input[i])
                {
                    passFlag = true;
                    break;
                }
                trimStr.Add(strs[i]);
            }

            int lastIndex = -1;
            //maxLengthが-1の場合(文節数が無制限に指定されている場合)はlastIndexをstrsの最後の要素数にする
            if(maxLength == -1)
            {
                lastIndex = strs.Length - 1;
            }
            else
            {
                lastIndex = maxLength;
            }
            //inputの文節量とmaxLengthの場所との差
            for (int i = input.Length ; i <= lastIndex; i++)
            {
                if(i <= strs.Length - 1)
                {
                    trimStr.Add(strs[i]);
                }
            }

            string mergeStr = string.Join(_separateMark, trimStr);
            //全単語が出力する文字列に含まれている&既にリストに追加されていない場合リストに追加
            if (!passFlag && !removedStrArray.Contains(mergeStr))
            {
                removedStrArray.Add(mergeStr);
            }
        }
        return removedStrArray;
    }

    private WordEmtCell DeepnTree(string[] inputStrArray, WordEmtCell rootWec, List<WordEmtCell> recordWecs)
    {
        //検索対象のオブジェクト(書きかけの文字列の１つ上)まで単語辞書の場所を深める
        recordWecs.Add(rootWec);
        WordEmtCell targetWec = rootWec;
        for (int i = 0; i < inputStrArray.Count() - 1; i++)
        {
            WordEmtCell finded = targetWec._childCells.Find(x => x._myWord == inputStrArray[i]);
            if (finded == null) return null;
            recordWecs.Add(finded);
            targetWec = finded;
        }
        return targetWec;
    }

    private List<WordEmtCell> SearchWords(string input, WordEmtCell parentCell)
    {
        List<WordEmtCell> hitWordCell = parentCell._childCells.Where(x => x._myWord.StartsWith(input)).ToList();
        return hitWordCell;
    }

    //WordEmtCellの子要素を繋げて文章を生成する再帰的メソッド
    private void ConnectToDeepth(WordEmtCell currentCell, string currentPath, int depth, List<string> returnStrs)
    {
        //depthの深さに達した場合、子要素がない場合は処理を終わる
        if(depth == 0 || currentCell._childCells == null || currentCell._childCells.Count == 0)
        {
            returnStrs.Add(currentPath);
            return;
        }

        foreach(WordEmtCell child in currentCell._childCells)
        {
            string nextPath = currentPath;
            //mywordが空の場合はパスに追加しない
            if (child._myWord != "")
            {
                nextPath += _separateMark + child._myWord;
            }

            ConnectToDeepth(child, nextPath, depth - 1, returnStrs);
        }
    }

    /// <summary>
    /// 指定された深さが、全体の文章で見た際に始めから何個目までを表しているのかを返す処理
    /// </summary>
    /// <return>1つめを0とした数</return>
    private int DepthToOverAll(int depth, int inputWordsLength)
    {
        int overAllIndex = 0;
        if(depth == -1)//最大まで返すよう指定されていた場合
        {
            overAllIndex = -1;
        }
        else
        {
            overAllIndex = (inputWordsLength - 1) + depth;
        }
        return overAllIndex;
    }
}

public static class WECLibCreater 
{
    public static WordEmtCell CreateLibFromStrList(List<string> targetStrList, string separateMark)
    {
        List<List<string>> splitStrsList = new List<List<string>>();
        int splitStrMaxLength = 0;
        foreach(string targetStr in targetStrList)
        {
            string[] splitStr = targetStr.Split(separateMark);
            if(splitStrMaxLength < splitStr.Length) splitStrMaxLength = splitStr.Length;
            splitStrsList.Add(splitStr.ToList());
        }

        WordEmtCell originCell = new WordEmtCell("", 0);
        foreach(List<string> strList in splitStrsList)
        {
            WordEmtCell parent = originCell;
            for(int i = 0; i < strList.Count; i++)
            {
                if(parent._childCells.Any(x => x._myWord == strList[i]))
                {
                    parent = parent._childCells.Find(x => x._myWord == strList[i]);
                    if(i == strList.Count - 1)
                    {
                        CreateEmptyWec(parent);
                    }
                }
                else
                {
                    WordEmtCell newWec = new WordEmtCell(strList[i], 0);
                    //最後の要素であった場合、印として子オブジェクトに空のwecを追加する
                    if(i == strList.Count - 1)
                    {
                        CreateEmptyWec(newWec);
                    }
                    parent.SetChild(newWec);
                    parent = newWec;
                }
            }
        }

        return originCell;
    }

    public static void CreateEmptyWec(WordEmtCell parent)
    {
        WordEmtCell newWec = new WordEmtCell("", 0);
        parent.SetChild(newWec);
    }

    public static WordEmtCell CreateLibFromLineAndPriority(Dictionary<string, int> words)
    {
        WordEmtCell originCell = new WordEmtCell("",0);
        foreach(var pair in words)
        {
            if (originCell._childCells.Any(x => x._myWord == pair.Key)) continue;
            WordEmtCell newWec = new WordEmtCell(pair.Key, pair.Value);
            originCell.SetChild(newWec);
        }
        return originCell;
    }
}

public class WordEmtCell 
{
    public string _myWord { get; private set; } = "";
    public List<WordEmtCell> _childCells { get; private set; } = new List<WordEmtCell>();
    public int _priority { get; private set; } = 0;

    public WordEmtCell(string myWord, int priority)
    {
        _myWord = myWord;
        _priority = priority;
    }

    public void SetChild(WordEmtCell wec)
    {
        _childCells.Add(wec);
    }
}

