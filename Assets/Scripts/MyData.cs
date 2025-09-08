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
            NofieldMessagePopUp("성별을 선택해주세요");
            return;
        }
        if (nickName == null)
        {
            NofieldMessagePopUp("닉네임을 입력해주세요");
            return;
        }
        if (ageUserInput == null)
        {
            NofieldMessagePopUp("나이을 입력해주세요");
            return;
        }
        if (personalityUserInput == null)
        {
            NofieldMessagePopUp("성격유형을 입력해주세요");
            return ;
        }
        if (job == null)
        {
            NofieldMessagePopUp("직업을 입력해주세요");
            return;
        }
        if (academic == null)
        {
            NofieldMessagePopUp("학력을 입력해주세요");
            return;
        }
        if (home == null)
        {
            NofieldMessagePopUp("거주지역을 입력해주세요");
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
            Debug.Log("숫자가 아닌 값이 포함되어 있습니다!");
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
        Debug.Log("저장 완료");
        myDataPanel.SetActive(false);
    }
    private void NofieldMessagePopUp(string _msg) => messageText.text = _msg;

}
