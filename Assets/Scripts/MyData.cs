using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyData : MonoBehaviour
{
    public TMP_InputField ageInputField;
    public TMP_InputField personalityInputField;

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
        string ageUserInput = ageInputField.text;
        string personalityUserInput = personalityInputField.text;
        if (gender == -1)
        {
            //Todo : popup message

            return;
        }
        if (ageUserInput == null)
        {
            //popUp message

            return;
        }
        if (personalityUserInput == null)
        {
            return ;
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

        
        PlayerPrefs.SetString("MBTI", personalityUserInput);
        Debug.Log("저장 완료");
        myDataPanel.SetActive(false);
    }
    

}
