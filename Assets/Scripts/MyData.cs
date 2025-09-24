using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyData : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;
    public TMP_InputField nickNameInputField;
    public TMP_InputField ageInputField;
    public TMP_InputField jobInputField;
    public TMP_InputField academicInputField;
    public TMP_InputField homeInputField;
    [SerializeField] Button saveBtn;

    string[] mbti = new string[] {"ISTP","ISFP","ISTJ","ISFJ","INFJ","INTJ","INFP","INTP",
        "ESFP","ESTP","ESFJ","ESTJ","ENTP","ENFP","ENFJ","ENTJ" };
    [HideInInspector] public string nickname, age, personality, job, academic, home;
    [HideInInspector] public int gender = -1;
    private void Awake()
    {
        saveBtn.onClick.AddListener(() => SaveDataOnClick());
    }
    private void OnEnable()
    {
        UpdateMyDataUI();
    }
    private void UpdateMyDataUI()
    {
        nickname = LobbyManager.instance.nickname;
        age = LobbyManager.instance.age;
        personality = LobbyManager.instance.personality;
        job = LobbyManager.instance.job;
        academic = LobbyManager.instance.academic;
        home = LobbyManager.instance.home;
        gender = LobbyManager.instance.gender;
        if (nickname != null) nickNameInputField.text = nickname;
        if (age != null) ageInputField.text = age;
        if (job != null) jobInputField.text = job;
        if (academic != null) academicInputField.text = academic;
        if (home != null) homeInputField.text = home;
    }

    public void GenderBtnClick(int _gender)
    {
        gender = _gender;
    }
    public void SaveDataOnClick()
    {
        Debug.Log("SaveOnClick");
        bool hasMBTI = false;
        int value;
        string nickNameIF = nickNameInputField.text;
        string ageIF = ageInputField.text;
        string jobIF = jobInputField.text;
        string academicIF = academicInputField.text;  
        string homeIF = homeInputField.text;
        if (gender == -1)
        {
            NofieldMessagePopUp("������ �������ּ���");
            return;
        }
        if (nickNameIF == null)
        {
            NofieldMessagePopUp("�г����� �Է����ּ���");
            return;
        }
        if (ageIF == null)
        {
            NofieldMessagePopUp("������ �Է����ּ���");
            return;
        }

        if (jobIF == null)
        {
            NofieldMessagePopUp("������ �Է����ּ���");
            return;
        }
        if (academicIF == null)
        {
            NofieldMessagePopUp("�з��� �Է����ּ���");
            return;
        }
        if (homeIF == null)
        {
            NofieldMessagePopUp("���������� �Է����ּ���");
            return;
        }

        //if(!hasMBTI) return;
        if (int.TryParse(ageIF, out value))
        {
            PlayerPrefs.SetInt("Age", int.Parse(ageIF));
        }
        else
        {
            Debug.Log("���ڰ� �ƴ� ���� ���ԵǾ� �ֽ��ϴ�!");
            return;
        }

        if (gender == 0) PlayerPrefs.SetInt("Gender", gender);
        else if (gender == 1) PlayerPrefs.SetInt("Gender", gender);
        PlayerPrefs.SetString("Nickname",nickNameIF);
        PlayerPrefs.SetString("Age",ageIF);
        PlayerPrefs.SetString("Job",jobIF);
        PlayerPrefs.SetString("Academic",academicIF);
        PlayerPrefs.SetString("Home",homeIF);
        //PlayerPrefs.SetString("Personality", personalityIF);
        Debug.Log(nickNameIF);
        Debug.Log(ageIF);
        Debug.Log("���� �Ϸ�");
        LobbyManager.instance.UpdateMyData();
        this.gameObject.SetActive(false);
    }
    private void NofieldMessagePopUp(string _msg)
    {
        Debug.Log(_msg);
        messageText.text = _msg;
    }

}
