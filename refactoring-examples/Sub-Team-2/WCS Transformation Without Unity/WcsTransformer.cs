using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WcsTransformer : IDisposable
{
    private IntPtr astFrameSet;          
    private bool disposed = false;


    public WcsTransformer(string fitsHeader, double restFreqGHz = 0.0)
    {
        int status = AstTool.InitAstFrameSet(out astFrameSet, 
                                             Marshal.StringToHGlobalAnsi(fitsHeader), 
                                             restFreqGHz);
        if (status != 0 || astFrameSet == IntPtr.Zero)
            throw new Exception("Failed to initialise AST FrameSet from FITS header.");
    }


    public void Dispose()
    {
        if (!disposed && astFrameSet != IntPtr.Zero)
        {
            AstTool.DeleteObject(astFrameSet);
            astFrameSet = IntPtr.Zero;
        }
        disposed = true;
        GC.SuppressFinalize(this);
    }

    ~WcsTransformer() => Dispose();


    public Vector3 InverseTransformPoint(Vector3 pixel)
    {
        return Transform(pixel, forward: 1);
    }


    public Vector3 TransformPoint(Vector3 world)
    {
        return Transform(world, forward: 0);
    }


    private Vector3 Transform(Vector3 input, int forward)
    {
        if (astFrameSet == IntPtr.Zero)
            throw new ObjectDisposedException("WcsTransformer");

        double xOut, yOut, zOut;
        int ret = AstTool.Transform3D(astFrameSet, input.x, input.y, input.z, forward,
                                      out xOut, out yOut, out zOut);
        if (ret != 0)
            throw new Exception("AST Transform3D failed.");

        return new Vector3((float)xOut, (float)yOut, (float)zOut);
    }


    public Vector3 InverseTransformDirection(Vector3 pixelDir)
    {
        return TransformDirection(pixelDir, forward: 1);
    }


    public Vector3 TransformDirection(Vector3 worldDir)
    {
        return TransformDirection(worldDir, forward: 0);
    }

    private Vector3 TransformDirection(Vector3 dir, int forward)
    {

        Vector3 refPixel = Vector3.zero;
        Vector3 refWorld = Transform(refPixel, forward: 1);
        return DirectionHelper(dir, forward, refPixel, refWorld);
    }



    private Vector3 DirectionHelper(Vector3 dir, int forward, Vector3 refPixel, Vector3 refWorld)
    {
        double eps = 1e-5; 
        Vector3 stepPointPixel = refPixel + dir.normalized * (float)eps;

        Vector3 stepPointWorld;
        if (forward == 1) 
            stepPointWorld = Transform(stepPointPixel, forward);
        else              
        {

            Vector3 stepPointWorld2 = refWorld + dir.normalized * (float)eps;
            Vector3 stepPointPixel2 = Transform(stepPointWorld2, forward: 0);
            Vector3 result = (stepPointPixel2 - refPixel) / (float)eps;
            return result.normalized;
        }

        Vector3 delta = (stepPointWorld - refWorld) / (float)eps;
        return delta.normalized;
    }


    public Vector3 InverseTransformVector(Vector3 pixelVec)
    {
        return TransformVector(pixelVec, forward: 1);
    }

    public Vector3 TransformVector(Vector3 worldVec)
    {
        return TransformVector(worldVec, forward: 0);
    }

    private Vector3 TransformVector(Vector3 vec, int forward)
    {
        Vector3 refPixel = Vector3.zero;
        Vector3 refWorld = Transform(refPixel, forward: 1);
        double eps = 1e-5;
        Vector3 stepPointPixel = refPixel + vec * (float)eps;

        Vector3 stepPointWorld;
        if (forward == 1)
            stepPointWorld = Transform(stepPointPixel, forward);
        else
        {
            Vector3 stepPointWorld2 = refWorld + vec * (float)eps;
            Vector3 stepPointPixel2 = Transform(stepPointWorld2, forward: 0);
            return (stepPointPixel2 - refPixel) / (float)eps;
        }

        return (stepPointWorld - refWorld) / (float)eps;
    }


}