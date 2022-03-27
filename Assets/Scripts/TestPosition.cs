using Mediapipe;
using System.Collections.Generic;
using UnityEngine;

public class TestPosition : MonoBehaviour
{
  private const float _YThreshold = 0.1f;
  private const float _AngleThreshold = 50f;
  private const float _FlyThreshold = 2f;
  private float _lastAngle = 0f;

  public void Check(LandmarkList value)
  {
    if (value == null)
    {
      return;
    }

    IList<Landmark> landmarks = value.Landmark;
    var shoulderLine = new Vector2(landmarks[11].X - landmarks[12].X, landmarks[11].Y - landmarks[12].Y);
    var rightArmLine = new Vector2(landmarks[14].X - landmarks[12].X, landmarks[14].Y - landmarks[12].Y);
    var leftArmLine = new Vector2(landmarks[13].X - landmarks[11].X, landmarks[13].Y - landmarks[11].Y);
    var leftAngle = Vector2.Angle(shoulderLine, leftArmLine);
    var rightAngle = Vector2.Angle(-shoulderLine, rightArmLine);
    var leftY = landmarks[13].Y;
    var rightY = landmarks[14].Y;
    var deltaY = leftY - rightY;
    //平飞 angle<50 && -0.1<y<0.1
    //左转  y>0.1
    //右转  y<-0.1
    //下降 angle>50 && -0.1<y<0.1
    if (deltaY > _YThreshold)
    {
      Debug.Log("Left");
    }
    else if (deltaY < -_YThreshold)
    {
      Debug.Log("Right");
    }
    else if (leftAngle < _AngleThreshold)
    {
      var deltaAngle = Mathf.Abs(_lastAngle - leftAngle) - _FlyThreshold;
      if (deltaAngle < 0f)
      {
        Debug.Log("Fly");
      }
      else
      {
        Debug.Log("Up " + deltaAngle);
      }
    }
    else
    {
      Debug.Log("Down");
    }
    _lastAngle = leftAngle;
  }
}
