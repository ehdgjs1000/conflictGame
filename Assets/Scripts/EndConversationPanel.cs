using UnityEngine;

public class EndConversationPanel : MonoBehaviour
{
    [SerializeField] private OpenAIChat AI;
    public void YesOnClick()
    {
        AI.EndConversation("대화를 종료하였습니다.");
        this.gameObject.SetActive(false);   
    }
    public void NoOnClick()
    {
        this.gameObject.SetActive(false);
    }

}
