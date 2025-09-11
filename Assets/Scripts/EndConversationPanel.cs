using UnityEngine;

public class EndConversationPanel : MonoBehaviour
{
    [SerializeField] private OpenAIChat AI;
    public void YesOnClick()
    {
        AI.EndConversation("��ȭ�� �����Ͽ����ϴ�.");
        this.gameObject.SetActive(false);   
    }
    public void NoOnClick()
    {
        this.gameObject.SetActive(false);
    }

}
