using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq.Expressions;

public class WordEstimaterSampleCode : MonoBehaviour
{
    //検索に利用されるリスト
    List<string> _testList = new List<string>
    {
        "im love you",
        "im love",
        "im love me",
        "im love my pet",
        "im love my boyfriend",
        "im like my brother",
        "im live in dream"
    };
    //検索で利用する単語と単語を区切る文字
    string _separateMark = " ";

    //検索に利用される優先度付きのリスト(1単語のみ)
    //※数値が高いほど優先度が高い
    Dictionary<string, int> _testDic = new Dictionary<string, int> 
    {
        { "きのこ", 5},
        { "きのぴお", 4},
        { "きこり", 3},
        { "きのこしがけ", 2},
        { "きんし", 0}
    };

    WordEmtCell _wecLibraryOfSentence = null;
    WordEmtCell _wecLibraryOfWord = null;
    WordEstimater _wordEstimaterOfSentence = null;
    WordEstimater _wordEstimaterOfWord = null;

    //検索時に入力した文章の何単語後まで検索を行うか。最小単位は0、-1だと入力から考えられる文章を全て返す
    [SerializeField] int _returnDepth = 0;
    //入力に利用するInputField
    [SerializeField] InputField _inputFieldOfSentence;
    [SerializeField] InputField _inputFieldOfWord;

    //結果を表示するためのテキストオブジェクトとその他
    [SerializeField] GameObject _resultUI;
    [SerializeField] float _resultUIWidth; //候補と候補の間隔
    [SerializeField] GameObject _uiCanvas;
    List<GameObject> _resultUIPool = new List<GameObject>();

    private void Awake()
    {
        Init();
    }

    //WordEstimaterを利用する際の初期化処理
    private void Init()
    {
        //WordEmtCell形式の単語辞書を作成する========================================================

        //(string型のList形式での登録はCreateLibFromStrListメソッドを使用)
        _wecLibraryOfSentence = WECLibCreater.CreateLibFromStrList(_testList, _separateMark);
        //(優先度付きのDictionary形式での登録はCreateLibFromLineAndPriorityを使用)
        _wecLibraryOfWord = WECLibCreater.CreateLibFromLineAndPriority(_testDic);

        //===========================================================================================

        //検索処理を実行するWordEstimaterクラスをインスタンス========================================

        _wordEstimaterOfSentence = new WordEstimater(_wecLibraryOfSentence, _separateMark);
        _wordEstimaterOfWord = new WordEstimater(_wecLibraryOfWord, _separateMark);

        //===========================================================================================

    }

    /// <summary>
    /// Sentence用のinputfieldの値が変更された時に呼ばれる
    /// </summary>
    public void ReceiveSentence()
    {
        //inputfieldの値を取得
        string valueFromIF = _inputFieldOfSentence.text;
        //予測変換を取得
        List<string> estimatedStrs = DoEstimateInSentence(valueFromIF);
        if(estimatedStrs == null) //予測候補が1つもない場合
        {
            //要素数0のリストを渡すことで全削除
            ReplaceUIPool(new List<string>());
        }
        else
        {
            //UIに反映
            ReflectForUI(estimatedStrs, _inputFieldOfSentence.gameObject);
        }
    }

    /// <summary>
    /// 文章での予測変換を実行
    /// </summary>
    private List<string> DoEstimateInSentence(string input)
    {
        List<string> estimatedStrs = null;

        //ReturnEstimatedStrsを用い予測変換を実行
        estimatedStrs = _wordEstimaterOfSentence.ReturnEstimatedStrs(input, _returnDepth);

        return estimatedStrs;
    }

    /// <summary>
    /// 単語での予測変換を実行(こっちは単語ごとの優先度に従い降順にリストを返す)
    /// </summary>
    private List<string> DoEstimateInWord(string input)
    {
        List<string> estimatedStrs = null;
        try
        {
            //ReturnEstimatedStrsを用い予測変換を実行
            //※単語検索は文章での予測が無いため、returnDepthは0
            estimatedStrs = _wordEstimaterOfWord.ReturnEstimatedStrs(input, 0);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        return estimatedStrs;
    }

    /// <summary>
    /// 予測結果をUIに反映させる
    /// </summary>
    private void ReflectForUI(List<string> estimatedList, GameObject inputFieldObject)
    {
        List<string>restStrs = ReplaceUIPool(estimatedList);
        if (restStrs == null || restStrs.Count == 0) return;

        Vector2 instancePos = inputFieldObject.transform.position;
        instancePos.x += (float)0.4; //表示位置調整用
        instancePos.y += (float)0.1; //表示位置調整用

        foreach (string estimated in estimatedList)
        {
            instancePos.y += _resultUIWidth;
            GameObject createUIObj = Instantiate(_resultUI, instancePos, Quaternion.identity, parent: _uiCanvas.transform);
            //値を反映
            createUIObj.GetComponent<Text>().text = estimated;
            _resultUIPool.Add(createUIObj);
        }
    }

    private List<string> ReplaceUIPool(List<string> newInput)
    {
        if(newInput.Count == 0)
        {
            foreach(GameObject g in _resultUIPool)
            {
                g.SetActive(false);
            }
            return new List<string>();
        }
        else if(newInput.Count <= _resultUIPool.Count)
        {
            for(int i = 0; i < _resultUIPool.Count; i++)
            {
                if(i <= newInput.Count - 1)
                {
                    _resultUIPool[i].GetComponent<Text>().text = newInput[i];
                    _resultUIPool[i].SetActive(true);
                }
                else
                {
                    _resultUIPool[i].SetActive(false);
                }
            }
            return new List<string>();
        }
        else if(newInput.Count > _resultUIPool.Count)
        {
            for(int i = 0; i < _resultUIPool.Count; i++)
            {
                _resultUIPool[i].GetComponent<Text>().text = newInput[i];
                _resultUIPool[i].SetActive(true);
            }
            List<string> returnStrs = new List<string>();
            //UIに反映されていないぶんを配列で返す
            for(int i = _resultUIPool.Count; i < newInput.Count; i++)
            {
                returnStrs.Add(newInput[i]);
            }
            return returnStrs;
        }
        else
        {
            throw new Exception("replace ui error");
        }
    }

}
