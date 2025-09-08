using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyData : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;
    public TMP_InputField nickNameInputField;
    public TMP_InputField ageInputField;
    public TMP_InputField personalityInputField;
    public TMP_InputField jobInputField;
    public TMP_InputField academicInputField;
    public TMP_InputField homeInputField;

    [SerializeField] GameObject myDataPanel;
    string[] mbti = new string[] {"ISTP","ISFP","ISTJ","ISFJ","INFJ","INTJ","INFP","INTP",
        "ESFP","ESTP","ESFJ","ESTJ","ENTP","ENFP","ENFJ","ENTJ" };
    int gender =-1;
    private void Start()
    {
        gender = -1;
    }
    public void GenderBtnClick(int _gender)
    {
        gender = _gender;
    }
    public void SaveDataOnClick()
    {
        bool hasMBTI = false;
        int value;
        string nickName = nickNameInputField.text;
        string ageUserInput = ageInputField.text;
        string personalityUserInput = personalityInputField.text;
        string job = jobInputField.text;
        string academic = academicInputField.text;  
        string home = homeInputField.text;
        if (gender == -1)
        {
            NofieldMessagePopUp("������ �������ּ���");
            return;
        }
        if (nickName == null)
        {
            NofieldMessagePopUp("�г����� �Է����ּ���");
            return;
        }
        if (ageUserInput == null)
        {
            NofieldMessagePopUp("������ �Է����ּ���");
            return;
        }
        if (personalityUserInput == null)
        {
            NofieldMessagePopUp("���������� �Է����ּ���");
            return ;
        }
        if (job == null)
        {
            NofieldMessagePopUp("������ �Է����ּ���");
            return;
        }
        if (academic == null)
        {
            NofieldMessagePopUp("�з��� �Է����ּ���");
            return;
        }
        if (home == null)
        {
            NofieldMessagePopUp("���������� �Է����ּ���");
            return;
        }
        foreach (string str in mbti)
        {
            if (str == personalityUserInput) hasMBTI = true;
        }
        if(!hasMBTI) return;
        if (int.TryParse(ageUserInput, out value))
        {
            PlayerPrefs.SetInt("Age", int.Parse(ageUserInput));
        }
        else
        {
            Debug.Log("���ڰ� �ƴ� ���� ���ԵǾ� �ֽ��ϴ�!");
            return;
        }

        if (gender == 0) PlayerPrefs.SetInt("Gender", gender);
        else if (gender == 1) PlayerPrefs.SetInt("Gender", gender);
        PlayerPrefs.SetString("Nickname",nickName);
        PlayerPrefs.SetString("Age",ageUserInput);
        PlayerPrefs.SetString("Job",job);
        PlayerPrefs.SetString("Academic",academic);
        PlayerPrefs.SetString("Home",home);
        
        PlayerPrefs.SetString("MBTI", personalityUserInput);
        Debug.Log("���� �Ϸ�");
        myDataPanel.SetActive(false);
    }
    private void NofieldMessagePopUp(string _msg) => messageText.text = _msg;

}
