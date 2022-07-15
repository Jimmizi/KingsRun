using UnityEngine;

//=============================================================
/// <summary>  A PID controller for Vector3 values.  </summary>
//==================================
public class Vector3PID
{
    //=============================================================
    /// <summary>  Accumulated integral of the error.    </summary>
    //==================================
    private Vector3 integral;

    //=============================================================
    /// <summary>  Cache of the previous value.          </summary>
    //==================================
    private Vector3 lastValue;

    //=============================================================
    /// <summary>  Proportional control variable.        </summary>
    //==================================
    public float p { get; set; }

    //=============================================================
    /// <summary>  Integral control variable.            </summary>
    //==================================
    public float i { get; set; }

    //=============================================================
    /// <summary>  Derivative control variable.          </summary>
    //==================================
    public float d { get; set; }

    //=============================================================
    /// <summary>Constructs a new Vector3 PID controller.</summary>
    /// <param name="pFactor"> Proportional factor.        </param>
    /// <param name="iFactor"> Integral factor.            </param>
    /// <param name="dFactor"> Derivative factor.          </param>
    //==================================
    public Vector3PID(float pFactor, float iFactor, float dFactor)
    {
        p = pFactor;
        i = iFactor;
        d = dFactor;
        integral = Vector3.zero;
        lastValue = Vector3.zero;
    }

    //=============================================================
    /// <summary>Updates the controller with a new signal.</summary>
    /// <param name="currentValue"> The new signal.        </param>
    /// <param name="timeFrame"> 
    ///     The time delta since the last update.          </param>
    /// <returns> The modified signal after calculations.</returns>
    //==================================
    public Vector3 Update(Vector3 currentValue, float timeFrame)
    {
        integral += currentValue * timeFrame;
        Vector3 derivative = (currentValue - lastValue) / timeFrame;
        lastValue = currentValue;

        return currentValue * p
               + integral * i
               + derivative * d;
    }
}