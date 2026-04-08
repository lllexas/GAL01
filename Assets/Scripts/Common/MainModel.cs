using UnityEngine;

public class MainModel : SingletonData<MainModel>
{
    public UserModel CurrentUser { get; private set; } = CreateBootstrappedUser();
    public string CurrentActiveStageID { get; private set; }

    public bool HasUser => CurrentUser != null;
    public bool IsInStage => !string.IsNullOrEmpty(CurrentActiveStageID);

    public void SetCurrentUser(UserModel user)
    {
        CurrentUser = user ?? CreateBootstrappedUser();
        SaveBoot.EnsureDefaultPacks(CurrentUser);
        Debug.Log($"<color=cyan>[MainModel]</color> 当前用户已更新: {CurrentUser.Metadata?.PlayerName ?? "null"}");
    }

    public void SetCurrentStage(string stageId)
    {
        CurrentActiveStageID = stageId;
        Debug.Log($"<color=yellow>[MainModel]</color> 当前活跃关卡: {stageId}");
    }

    public void ClearCurrentStage()
    {
        string lastStage = CurrentActiveStageID;
        CurrentActiveStageID = null;
        Debug.Log($"<color=orange>[MainModel]</color> 已清空关卡状态，撤离 {lastStage}");
    }

    public void ClearCurrentUser()
    {
        CurrentUser = CreateBootstrappedUser();
        CurrentActiveStageID = null;
        Debug.Log("<color=red>[MainModel]</color> 当前用户数据已清空");
    }

    private static UserModel CreateBootstrappedUser()
    {
        var user = new UserModel();
        SaveBoot.EnsureDefaultPacks(user);
        return user;
    }
}
