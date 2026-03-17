using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoteResult
{
    public string cloudName;
    public int score;
    public float reactionTime;
    public string timestamp;
}

[System.Serializable]
public class VotePackage
{
    public List<VoteResult> results;
    public VotePackage(List<VoteResult> r) { results = r; }
}

