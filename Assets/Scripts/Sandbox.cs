using TMPro;
using UnityEngine;

public class Sandbox : MonoBehaviour
{
    public Player player;
    public TextMeshProUGUI textMeshPro;

    //async void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        Debug.Log("Injecting new behavior...");

    //        //string movementCode = @"
    //        //    float h = Input.GetAxis(""Horizontal"");
    //        //    float v = Input.GetAxis(""Vertical"");
    //        //    player.transform.position += new Vector3(h, 0, v) * Time.deltaTime * 5f;
    //        //";
    //        string movementCode = textMeshPro.text.ToString();
    //        Debug.Log($"{movementCode}");

    //        //var behavior = await RuntimeCompiler.CompileBehavior(movementCode);
    //        //player.SetBehavior(behavior);
    //    }
    //}
}
