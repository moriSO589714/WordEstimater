using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public class WordEstimater
{
    private WordEmtCell _wordsLib;
    private string _separateMark;

    private string _oldInput = "";
    private int _oldDepth;
    public WordEstimater(WordEmtCell wordsLib, string separateMark)
    {
        _wordsLib = wordsLib;
        _separateMark = separateMark;
    }

    private void SetDatas(string oldInput, int oldDepth)
    {
        _oldInput = oldInput;
        _oldDepth = oldDepth;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="returnDepth">入力しかけの単語以降、何単語目までの候補を返すか(0なら書きかけの単語部のみ)</param>
    /// <returns></returns>
    public List<string> ReturnEstimatedStrs(string input, int returnDepth)
    {
        string[] splitInput = input.Split(_separateMark, StringSplitOptions.RemoveEmptyEntries);

        WordEmtCell searchStartCell = _wordsLib;
        List<WordEmtCell> candidateCells = new List<WordEmtCell>();
        if (splitInput.Length == 1)
        {
            candidateCells = SearchWords(splitInput[0], searchStartCell);
        }
        else if (1 < splitInput.Length)
        {
            searchStartCell = DeepnTree(splitInput, searchStartCell);
            //指定の語が含まれていなかった場合
            if (searchStartCell == null) return null;
            string lastWord = splitInput.Last();
            candidateCells = SearchWords(lastWord, searchStartCell);
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
            if (splitInput.Length != 1)
            {
                beforePath = String.Join(_separateMark, splitInput.Take(splitInput.Length - 1).ToArray()) + _separateMark;
            }

            ConnectToDeepth(wec, beforePath + wec._myWord, returnDepth, candidateStrs);
            returnList.AddRange(candidateStrs);
        }

        SetDatas(input, returnDepth);
        if(returnList == null || returnList.Count == 0)
        {
            return null;
        }
        return returnList;
    }

    private WordEmtCell DeepnTree(string[] inputStrArray, WordEmtCell rootWec)
    {
        //検索対象のオブジェクト(書きかけの文字列の１つ上)まで単語辞書の場所を深める
        WordEmtCell targetWec = rootWec;
        for (int i = 0; i < inputStrArray.Count() - 1; i++)
        {
            WordEmtCell finded = targetWec._childCells.Find(x => x._myWord == inputStrArray[i]);
            if (finded == null) return null;
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
        if (depth == 0 || currentCell._childCells == null || currentCell._childCells.Count == 0)
        {
            returnStrs.Add(currentPath);
            return;
        }

        foreach (WordEmtCell child in currentCell._childCells)
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
}

    public static class WECLibCreater
    {
        public static WordEmtCell CreateLibFromStrList(List<string> targetStrList, string separateMark)
        {
            List<List<string>> splitStrsList = new List<List<string>>();
            int splitStrMaxLength = 0;
            foreach (string targetStr in targetStrList)
            {
                string[] splitStr = targetStr.Split(separateMark);
                if (splitStrMaxLength < splitStr.Length) splitStrMaxLength = splitStr.Length;
                splitStrsList.Add(splitStr.ToList());
            }

            WordEmtCell originCell = new WordEmtCell("", 0);
            foreach (List<string> strList in splitStrsList)
            {
                WordEmtCell parent = originCell;
                for (int i = 0; i < strList.Count; i++)
                {
                    if (parent._childCells.Any(x => x._myWord == strList[i]))
                    {
                        parent = parent._childCells.Find(x => x._myWord == strList[i]);
                        if (i == strList.Count - 1)
                        {
                            CreateEmptyWec(parent);
                        }
                    }
                    else
                    {
                        WordEmtCell newWec = new WordEmtCell(strList[i], 0);
                        //最後の要素であった場合、印として子オブジェクトに空のwecを追加する
                        if (i == strList.Count - 1)
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
            WordEmtCell originCell = new WordEmtCell("", 0);
            foreach (var pair in words)
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

