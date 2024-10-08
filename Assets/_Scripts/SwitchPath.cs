using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchPath : ScriptableObject
{

    private float switchLenght;
    private float switchBracketsLenght;
    private float centerWidth;
    private float tunnelWidth;
    private float switchLightDistance;
    private float switchRailsWidth;
    private float switchLightHeight;
    private Vector3 switchLightRotation;
    private GameObject switchLight;
    private int curvePointsNumber;

    public static SwitchPath CreateInstance( float switchRailsWidth, float switchLenght, float switchBracketsLenght, float centerWidth, float tunnelWidth, float switchLightDistance, float switchLightHeight, int curvePointsNumber, Vector3 switchLightRotation, GameObject switchLight ) {
        
        SwitchPath switchPathScript = ScriptableObject.CreateInstance<SwitchPath>();
        switchPathScript.switchRailsWidth = switchRailsWidth;
        switchPathScript.switchLenght = switchLenght;
        switchPathScript.centerWidth = centerWidth;
        switchPathScript.tunnelWidth = tunnelWidth;
        switchPathScript.switchLightDistance = switchLightDistance;
        switchPathScript.switchLightHeight = switchLightHeight;
        switchPathScript.curvePointsNumber = curvePointsNumber;
        switchPathScript.switchLightRotation = switchLightRotation;
        switchPathScript.switchLight = switchLight;
        switchPathScript.switchBracketsLenght = switchBracketsLenght;

        return switchPathScript;
    }

    public LineSection GenerateBiToBiSwitch( List<LineSection> sections, Vector3 startingDir, Vector3 startingPoint, GameObject sectionGameObj ) {

        LineSection section = new();
        section.type = Type.Switch;
        section.switchType = SwitchType.BiToBi;
        section.bidirectional = true;

        MeshGenerator.Floor switchFloor = new();
        Vector3 switchDir = startingDir.normalized;

        Dictionary<SwitchDirection, List<GameObject>> switchLights = new();

        Vector3 c0 = startingPoint;
        Vector3 r0 = c0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
        Vector3 l0 = c0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );

        Vector3 lb = l0 + switchDir * ( this.switchLenght / 2 ); 
        Vector3 rb = r0 + switchDir * ( this.switchLenght / 2 ); 

        Vector3 c1 = c0 + switchDir * this.switchLenght;
        Vector3 l1 = l0 + switchDir * this.switchLenght; 
        Vector3 r1 = r0 + switchDir * this.switchLenght;

        GameObject lightR0 = Instantiate( this.switchLight, r0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f , this.switchLightRotation.y, this.switchLightRotation.z ) );
        lightR0.transform.parent = sectionGameObj.transform;
        lightR0.name = "Semaforo R0";
        GameObject lightR1 = Instantiate( this.switchLight, r1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
        lightR1.transform.parent = sectionGameObj.transform;
        lightR1.name = "Semaforo R1";
        GameObject lightL0 = Instantiate( this.switchLight, l0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
        lightL0.transform.parent = sectionGameObj.transform;
        lightL0.name = "Semaforo L0";
        GameObject lightL1 = Instantiate( this.switchLight, l1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
        lightL1.transform.parent = sectionGameObj.transform;
        lightL1.name = "Semaforo L1";
        switchLights.Add( SwitchDirection.RightToRight, new List<GameObject>{ lightR0, lightR1 } );
        switchLights.Add( SwitchDirection.RightToLeft, new List<GameObject>{ lightR0, lightL1 } );
        switchLights.Add( SwitchDirection.LeftToLeft, new List<GameObject>{ lightL0, lightL1 } );
        switchLights.Add( SwitchDirection.LeftToRight, new List<GameObject>{ lightL0, lightR1 } );
        section.switchLights = switchLights;

        List<Vector3> rightLeftLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ r0, rb, lb, l1 }, this.curvePointsNumber );
        List<Vector3> leftRightLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ l0, lb, rb, r1 }, this.curvePointsNumber );

        switchFloor.leftRightLine = leftRightLine;
        switchFloor.rightLeftLine = rightLeftLine;

        switchFloor.leftLine = new List<Vector3>{ l0, lb, l1 };
        switchFloor.centerLine = new List<Vector3>{ c0, c1 };
        switchFloor.rightLine = new List<Vector3>{ r0, rb, r1 };

        MeshGenerator.SpecularBaseLine railLeftLine = MeshGenerator.CalculateBaseLinesFromCurve( switchFloor.leftLine, null, switchRailsWidth / 2, 0.0f );
        MeshGenerator.SpecularBaseLine railRightLine = MeshGenerator.CalculateBaseLinesFromCurve( switchFloor.rightLine, null, switchRailsWidth / 2, 0.0f );
        MeshGenerator.SpecularBaseLine railRightLeftLine = MeshGenerator.CalculateBaseLinesFromCurve( switchFloor.rightLeftLine, null, switchRailsWidth / 2, 0.0f );
        MeshGenerator.SpecularBaseLine railLeftRightLine = MeshGenerator.CalculateBaseLinesFromCurve( switchFloor.leftRightLine, null, switchRailsWidth / 2, 0.0f );

        switchFloor.railLeftL = railLeftLine.left;
        switchFloor.railLeftR = railLeftLine.right;
        switchFloor.railRightL = railRightLine.left;
        switchFloor.railRightR = railRightLine.right;
        switchFloor.railSwitchLeftRightL = railLeftRightLine.left;
        switchFloor.railSwitchLeftRightR = railLeftRightLine.right;
        switchFloor.railSwitchRightLeftL = railRightLeftLine.left;
        switchFloor.railSwitchRightLeftR = railRightLeftLine.right;

        List<Vector3> nextStartingDirections = new();
        nextStartingDirections.Add( startingDir );
        section.nextStartingDirections = nextStartingDirections;

        List<Vector3> nextStartingPoints = new();
        nextStartingPoints.Add( c1 );
        section.nextStartingPoints = nextStartingPoints;

        section.nextStartingDirections = nextStartingDirections; 
        section.nextStartingPoints =  nextStartingPoints;
        section.floorPoints = switchFloor;

        section.activeSwitch = SwitchDirection.RightToRight;
        section.curvePointsCount = switchFloor.rightLine.Count;
        section.floorPoints = switchFloor;

        TrainController.UpdateSwitchLight( section );

        return section;
    }

    public LineSection GenerateBiToMonoSwitch( List<LineSection> sections, Vector3 startingDir, Vector3 startingPoint, GameObject sectionGameObj ) {

        LineSection section = new();
        section.type = Type.Switch;
        section.switchType = SwitchType.BiToMono;
        section.bidirectional = false;

        MeshGenerator.Floor switchFloor = new();
        Vector3 switchDir = startingDir.normalized;

        Dictionary<SwitchDirection, List<GameObject>> switchLights = new();
        Vector3 c0, r0, l0, cb, rb, lb, c1/*, r1, l1*/;
        List<Vector3> nextStartingPoints = new();
        c0 = startingPoint;
        cb = c0 + switchDir * ( this.switchLenght / 2 );
        c1 = c0 + switchDir * this.switchLenght;

        //int variant = Random.Range( 0, 3 );
        //if( variant == 0 ) { // Allineamento centrale
            r0 = c0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
            l0 = c0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );

            cb = c0 + switchDir * ( this.switchLenght / 2 );
            lb = l0 + switchDir * ( this.switchLenght / 2 ); 
            rb = r0 + switchDir * ( this.switchLenght / 2 ); 

            GameObject lightR0 = Instantiate( this.switchLight, r0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f , this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR0.transform.parent = sectionGameObj.transform;
            lightR0.name = "Semaforo R0";
            GameObject lightL0 = Instantiate( this.switchLight, l0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL0.transform.parent = sectionGameObj.transform;
            lightL0.name = "Semaforo L0";
            GameObject lightC1 = Instantiate( this.switchLight, c1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightC1.transform.parent = sectionGameObj.transform;
            lightC1.name = "Semaforo C1";
            switchLights.Add( SwitchDirection.RightToCenter, new List<GameObject>{ lightR0, lightC1 } );
            switchLights.Add( SwitchDirection.LeftToCenter, new List<GameObject>{ lightL0, lightC1 } );
            section.switchLights = switchLights;

            List<Vector3> rightCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ r0, rb, cb, c1 }, this.curvePointsNumber );
            List<Vector3> leftCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ l0, lb, cb, c1 }, this.curvePointsNumber );

            

            switchFloor.rightCenterLine = rightCenterLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.leftCenterLine = leftCenterLine;

            nextStartingPoints.Add( c1 );
            section.nextStartingPoints = nextStartingPoints;
        /*}
        else if( variant == 1 ) { // Allineamento sinistro
            r0 = c0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
            l0 = c0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );
            l1 = l0 + switchDir * this.switchLenght;

            cb = c0 + switchDir * ( this.switchLenght / 2 );
            lb = l0 + switchDir * ( this.switchLenght / 2 ); 
            rb = r0 + switchDir * ( this.switchLenght / 2 ); 

            GameObject lightR0 = Instantiate( this.switchLight, r0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f , this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR0.transform.parent = sectionGameObj.transform;
            lightR0.name = "Semaforo R0";
            GameObject lightL0 = Instantiate( this.switchLight, l0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL0.transform.parent = sectionGameObj.transform;
            lightL0.name = "Semaforo L0";
            GameObject lightL1 = Instantiate( this.switchLight, l1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL1.transform.parent = sectionGameObj.transform;
            lightL1.name = "Semaforo L1";
            switchLights.Add( SwitchDirection.RightToCenter, new List<GameObject>{ lightR0, lightL1 } );
            switchLights.Add( SwitchDirection.LeftToCenter, new List<GameObject>{ lightL0, lightL1 } );
            section.switchLights = switchLights;

            List<Vector3> rightCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ r0, rb, lb, l1 }, this.curvePointsNumber );
            List<Vector3> leftCenterLine = new List<Vector3>{ l0, lb, l1 };

            switchFloor.rightCenterLine = rightCenterLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.leftCenterLine = leftCenterLine;

            nextStartingPoints.Add( l1 );
            section.nextStartingPoints = nextStartingPoints;
        }
        else if( variant == 2 ) { // Allineamento destro
            r0 = c0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
            l0 = c0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );
            r1 = r0 + switchDir * this.switchLenght;

            cb = c0 + switchDir * ( this.switchLenght / 2 );
            lb = l0 + switchDir * ( this.switchLenght / 2 ); 
            rb = r0 + switchDir * ( this.switchLenght / 2 ); 

            GameObject lightR0 = Instantiate( this.switchLight, r0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f , this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR0.transform.parent = sectionGameObj.transform;
            lightR0.name = "Semaforo R0";
            GameObject lightL0 = Instantiate( this.switchLight, l0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL0.transform.parent = sectionGameObj.transform;
            lightL0.name = "Semaforo L0";
            GameObject lightR1 = Instantiate( this.switchLight, r1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR1.transform.parent = sectionGameObj.transform;
            lightR1.name = "Semaforo R1";
            switchLights.Add( SwitchDirection.RightToCenter, new List<GameObject>{ lightR0, lightR1 } );
            switchLights.Add( SwitchDirection.LeftToCenter, new List<GameObject>{ lightL0, lightR1 } );
            section.switchLights = switchLights;

            List<Vector3> rightCenterLine = new List<Vector3>{ r0, rb, r1 };
            List<Vector3> leftCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ l0, lb, rb, r1 }, this.curvePointsNumber );

            switchFloor.rightCenterLine = rightCenterLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.leftCenterLine = leftCenterLine;

            nextStartingPoints.Add( r1 );
            section.nextStartingPoints = nextStartingPoints;
        }*/

        List<Vector3> nextStartingDirections = new();
        nextStartingDirections.Add( startingDir );
        section.nextStartingDirections = nextStartingDirections;

        section.nextStartingDirections = nextStartingDirections; 
        section.nextStartingPoints =  nextStartingPoints;

        section.activeSwitch = SwitchDirection.RightToCenter;
        section.curvePointsCount = switchFloor.rightCenterLine.Count;
        section.floorPoints = switchFloor;

        TrainController.UpdateSwitchLight( section );

        return section;
    }

    public LineSection GenerateMonoToBiSwitch( List<LineSection> sections, Vector3 startingDir, Vector3 startingPoint, GameObject sectionGameObj ) {

        LineSection section = new();
        section.type = Type.Switch;
        section.switchType = SwitchType.MonoToBi;
        section.bidirectional =  true;

        MeshGenerator.Floor switchFloor = new();
        Vector3 switchDir = startingDir.normalized;

         Dictionary<SwitchDirection, List<GameObject>> switchLights = new();
        Vector3 c0, /*r0, l0,*/ cb, rb, lb, c1, r1, l1;
        List<Vector3> nextStartingPoints = new();

        int variant = Random.Range( 0, 3 );
        //if( variant == 0 ) { // Allineamento centrale
            c0 = startingPoint;
            cb = c0 + switchDir * ( this.switchLenght / 2 );
            c1 = c0 + switchDir * this.switchLenght;

            r1 = c1 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
            l1 = c1 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );

            lb = l1 - switchDir * ( this.switchLenght / 2 ); 
            rb = r1 - switchDir * ( this.switchLenght / 2 ); 

            GameObject lightR1 = Instantiate( this.switchLight, r1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) , this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR1.transform.parent = sectionGameObj.transform;
            lightR1.name = "Semaforo R1";
            GameObject lightL1 = Instantiate( this.switchLight, l1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL1.transform.parent = sectionGameObj.transform;
            lightL1.name = "Semaforo L1";
            GameObject lightC0 = Instantiate( this.switchLight, c0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightC0.transform.parent = sectionGameObj.transform;
            lightC0.name = "Semaforo C0";
            switchLights.Add( SwitchDirection.RightToCenter, new List<GameObject>{ lightC0, lightR1 } );
            switchLights.Add( SwitchDirection.LeftToCenter, new List<GameObject>{ lightC0, lightL1 } );
            section.switchLights = switchLights;

            List<Vector3> rightCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c0, cb, rb, r1 }, this.curvePointsNumber );
            List<Vector3> leftCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c0, cb, lb, l1 }, this.curvePointsNumber );

            switchFloor.rightCenterLine = rightCenterLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.leftCenterLine = leftCenterLine;

            nextStartingPoints.Add( c1 );
            section.nextStartingPoints = nextStartingPoints;
        /*}
        else if( variant == 1 ) { // Allineamento sinistro
            l0 = startingPoint;
            c0 = l0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );
            r0 = l0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( this.centerWidth + this.tunnelWidth );

            c1 = c0 + switchDir * this.switchLenght;
            r1 = c1 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
            l1 = c1 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );

            cb = c0 + switchDir * ( this.switchLenght / 2 ) ;
            lb = l1 - switchDir * ( this.switchLenght / 2 ); 
            rb = r1 - switchDir * ( this.switchLenght / 2 ); 

            GameObject lightR1 = Instantiate( this.switchLight, r1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) , this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR1.transform.parent = sectionGameObj.transform;
            lightR1.name = "Semaforo R1";
            GameObject lightL1 = Instantiate( this.switchLight, l1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL1.transform.parent = sectionGameObj.transform;
            lightL1.name = "Semaforo L1";
            GameObject lightL0 = Instantiate( this.switchLight, l0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL0.transform.parent = sectionGameObj.transform;
            lightL0.name = "Semaforo L0";
            switchLights.Add( SwitchDirection.RightToCenter, new List<GameObject>{ lightL0, lightR1 } );
            switchLights.Add( SwitchDirection.LeftToCenter, new List<GameObject>{ lightL0, lightL1 } );
            section.switchLights = switchLights;

            List<Vector3> rightCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ l0, lb, rb, r1 }, this.curvePointsNumber );
            List<Vector3> leftCenterLine = new List<Vector3>{ l0, lb, l1 };

            switchFloor.rightCenterLine = rightCenterLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.leftCenterLine = leftCenterLine;

            nextStartingPoints.Add( c1 );
            section.nextStartingPoints = nextStartingPoints;
        }
        else if( variant == 2 ) { // Allineamento sinistro
            r0 = startingPoint;
            c0 = r0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );
            l0 = r0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( this.centerWidth + this.tunnelWidth );

            c1 = c0 + switchDir * this.switchLenght;
            r1 = c1 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * ( ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) ); 
            l1 = c1 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * ( (this.centerWidth / 2 ) + ( this.tunnelWidth / 2 ) );

            cb = c0 + switchDir * ( this.switchLenght / 2 ) ;
            lb = l1 - switchDir * ( this.switchLenght / 2 ); 
            rb = r1 - switchDir * ( this.switchLenght / 2 ); 

            GameObject lightR1 = Instantiate( this.switchLight, r1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) , this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR1.transform.parent = sectionGameObj.transform;
            lightR1.name = "Semaforo R1";
            GameObject lightL1 = Instantiate( this.switchLight, l1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightL1.transform.parent = sectionGameObj.transform;
            lightL1.name = "Semaforo L1";
            GameObject lightR0 = Instantiate( this.switchLight, r0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * startingDir.normalized * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightR0.transform.parent = sectionGameObj.transform;
            lightR0.name = "Semaforo R0";
            switchLights.Add( SwitchDirection.RightToCenter, new List<GameObject>{ lightR0, lightR1 } );
            switchLights.Add( SwitchDirection.LeftToCenter, new List<GameObject>{ lightR0, lightL1 } );
            section.switchLights = switchLights;

            List<Vector3> rightCenterLine = new List<Vector3>{ r0, rb, r1 };
            List<Vector3> leftCenterLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ r0, rb, lb, l1 }, this.curvePointsNumber );

            switchFloor.rightCenterLine = rightCenterLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.leftCenterLine = leftCenterLine;

            nextStartingPoints.Add( c1 );
            section.nextStartingPoints = nextStartingPoints;
        }*/

        List<Vector3> nextStartingDirections = new() { startingDir };
        section.nextStartingDirections = nextStartingDirections;

        section.nextStartingDirections = nextStartingDirections; 
        section.nextStartingPoints =  nextStartingPoints;

        section.activeSwitch = SwitchDirection.RightToCenter;
        section.curvePointsCount = switchFloor.rightCenterLine.Count;
        section.floorPoints = switchFloor;

        TrainController.UpdateSwitchLight( section );

        return section;
    }

    /*public LineSection GenerateMonoToNewMonoSwitch( int index, List<LineSection> sections, Vector3 startingDir, Vector3 startingPoint, GameObject sectionGameObj ) {

        LineSection section = new LineSection();
        section.type = Type.Switch;
        section.switchType = SwitchType.MonoToNewMono;
        section.bidirectional =  false;
        section.newBidirectional =  false;

        MeshGenerator.Floor switchFloor = new MeshGenerator.Floor();
        Vector3 switchDir = startingDir.normalized;

         Dictionary<SwitchDirection, List<GameObject>> switchLights = new Dictionary<SwitchDirection, List<GameObject>>();
        Vector3 c0, cb, c1, nr1, nl1;
        List<Vector3> nextStartingPoints = new List<Vector3>();
        List<Vector3> nextStartingDirections = new List<Vector3>();

        int variant = Random.Range( 0, 2 );
        if( variant == 0 ) { // Nuova linea a sinistra
            c0 = startingPoint;
            cb = c0 + switchDir * ( this.switchLenght / 2 );
            c1 = c0 + switchDir * this.switchLenght;

            float alpha = Random.Range( -135.0f, -45.0f );

            nr1 = cb + Quaternion.Euler( 0.0f, 0.0f, alpha ) * switchDir * this.switchLenght; 

            GameObject lightC0 = Instantiate( this.switchLight, c0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightC0.transform.parent = sectionGameObj.transform;
            lightC0.name = "Semaforo C0";
            GameObject lightC1 = Instantiate( this.switchLight, c1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightC1.transform.parent = sectionGameObj.transform;
            lightC1.name = "Semaforo C1";
            GameObject lightNR1 = Instantiate( this.switchLight, nr1 + ( Quaternion.Euler( 0.0f, 0.0f, 0.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) + alpha, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightNR1.transform.parent = sectionGameObj.transform;
            lightNR1.name = "Semaforo NR1";
            switchLights.Add( SwitchDirection.CenterToCenter, new List<GameObject>{ lightC0, lightC1 } );
            switchLights.Add( SwitchDirection.CenterToNewLineForward, new List<GameObject>{ lightC0, lightNR1 } );
            switchLights.Add( SwitchDirection.CenterToNewLineBackward, new List<GameObject>{ lightC1, lightNR1 } );
            section.switchLights = switchLights;

            List<Vector3> leftCenterNewRightLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c0, cb, nr1 }, this.curvePointsNumber );
            List<Vector3> rightCenterNewRightLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c1, cb, nr1 }, this.curvePointsNumber );

            switchFloor.leftCenterNewLine = leftCenterNewRightLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.rightCenterNewLine = rightCenterNewRightLine;

            nextStartingPoints.Add( c1 );
            nextStartingPoints.Add( nr1 );
            section.nextStartingPoints = nextStartingPoints;

            nextStartingDirections.Add( startingDir );
            nextStartingDirections.Add( nr1 - cb );
            section.nextStartingDirections = nextStartingDirections;
        }
        else if( variant == 1 ) { // Nuova linea a destra
            c0 = startingPoint;
            cb = c0 + switchDir * ( this.switchLenght / 2 );
            c1 = c0 + switchDir * this.switchLenght;

            float alpha = Random.Range( 45.0f, 135.0f );

            nl1 = cb + Quaternion.Euler( 0.0f, 0.0f, alpha ) * switchDir * this.switchLenght; 

            GameObject lightC0 = Instantiate( this.switchLight, c0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightC0.transform.parent = sectionGameObj.transform;
            lightC0.name = "Semaforo C0";
            GameObject lightC1 = Instantiate( this.switchLight, c1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightC1.transform.parent = sectionGameObj.transform;
            lightC1.name = "Semaforo C1";
            GameObject lightNL1 = Instantiate( this.switchLight, nl1 + ( Quaternion.Euler( 0.0f, 0.0f, -180.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) + alpha, this.switchLightRotation.y, this.switchLightRotation.z ) );
            lightNL1.transform.parent = sectionGameObj.transform;
            lightNL1.name = "Semaforo NL1";
            switchLights.Add( SwitchDirection.CenterToCenter, new List<GameObject>{ lightC0, lightC1 } );
            switchLights.Add( SwitchDirection.CenterToNewLineForward, new List<GameObject>{ lightC0, lightNL1 } );
            switchLights.Add( SwitchDirection.CenterToNewLineBackward, new List<GameObject>{ lightC1, lightNL1 } );
            section.switchLights = switchLights;

            List<Vector3> leftCenterNewRightLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c0, cb, nl1 }, this.curvePointsNumber );
            List<Vector3> rightCenterNewRightLine = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c1, cb, nl1 }, this.curvePointsNumber );

            switchFloor.leftCenterNewLine = leftCenterNewRightLine;
            switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };
            switchFloor.rightCenterNewLine = rightCenterNewRightLine;

            nextStartingPoints.Add( c1 );
            nextStartingPoints.Add( nl1 );
            section.nextStartingPoints = nextStartingPoints;

            nextStartingDirections.Add( startingDir );
            nextStartingDirections.Add( nl1 - cb );
            section.nextStartingDirections = nextStartingDirections;
        }

        section.nextStartingDirections = nextStartingDirections; 
        section.nextStartingPoints =  nextStartingPoints;
        section.floorPoints = switchFloor;

        section.activeSwitch = SwitchDirection.CenterToCenter;
        section.curvePointsCount = switchFloor.centerLine.Count;
        section.floorPoints = switchFloor;

        return section;
    }*/

    public LineSection GenerateMonoToNewMonoSwitch( LineSection switchSection, Vector3 startingDir, Vector3 startingPoint, GameObject sectionGameObj ) {

        LineSection section = new();
        section.type = Type.Switch;
        section.switchType = SwitchType.MonoToNewMono;
        section.bidirectional =  false;
        section.newBidirectional =  false;

        MeshGenerator.Floor switchFloor = new();
        Vector3 switchDir = startingDir.normalized;

        Dictionary<SwitchDirection, List<GameObject>> switchLights = new();
        Vector3 c0, cb, c1, nr1, nl1;
        List<Vector3> nextStartingPoints = new();
        List<Vector3> nextStartingDirections = new();

        c0 = startingPoint;
        cb = c0 + switchDir * ( this.switchLenght / 2 );
        c1 = c0 + switchDir * this.switchLenght;

        GameObject lightC0 = Instantiate( this.switchLight, c0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
        lightC0.transform.parent = sectionGameObj.transform;
        lightC0.name = "Semaforo C0";
        GameObject lightC1 = Instantiate( this.switchLight, c1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
        lightC1.transform.parent = sectionGameObj.transform;
        lightC1.name = "Semaforo C1";
        switchLights.Add( SwitchDirection.CenterToCenter, new List<GameObject>{ lightC0, lightC1 } );

        nextStartingPoints.Add( c1 );
        nextStartingDirections.Add( startingDir );

        switchFloor.centerLine = new List<Vector3>{ c0, cb, c1 };

        foreach( Side newSide in switchSection.newLinesStarts.Keys ) {

            if( newSide == Side.Right ) { // Nuova linea a Destra
                
                nr1 = switchSection.newLinesStarts[ newSide ].pos; 

                GameObject lightNR1 = Instantiate( this.switchLight, nr1 + ( Quaternion.Euler( 0.0f, 0.0f, 0.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( switchSection.newLinesStarts[ newSide ].dir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightNR1.transform.parent = sectionGameObj.transform;
                lightNR1.name = "Semaforo NR1";
                switchLights.Add( SwitchDirection.CenterToEntranceRight, new List<GameObject>{ lightC0, lightNR1 } );
                switchLights.Add( SwitchDirection.CenterToExitRight, new List<GameObject>{ lightC1, lightNR1 } );

                List<Vector3> forwardCenterNewRight = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c0, cb, nr1 }, this.curvePointsNumber );
                List<Vector3> backwardCenterNewRight = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ nr1, cb, c1 }, this.curvePointsNumber );

                switchFloor.centerEntranceRight = forwardCenterNewRight;
                switchFloor.centerExitRight = backwardCenterNewRight;

                nextStartingPoints.Add( nr1 );

                nextStartingDirections.Add( nr1 - cb );
            }
            else if( newSide == Side.Left ) { // Nuova linea a Sinistra

                nl1 = switchSection.newLinesStarts[ newSide ].pos;

                GameObject lightNL1 = Instantiate( this.switchLight, nl1 + ( Quaternion.Euler( 0.0f, 0.0f, -180.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( switchSection.newLinesStarts[ newSide ].dir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightNL1.transform.parent = sectionGameObj.transform;
                lightNL1.name = "Semaforo NL1";
                switchLights.Add( SwitchDirection.CenterToEntranceLeft, new List<GameObject>{ lightC0, lightNL1 } );
                switchLights.Add( SwitchDirection.CenterToExitLeft, new List<GameObject>{ lightC1, lightNL1 } );

                List<Vector3> forwardCenterNewLeft = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ c0, cb, nl1 }, this.curvePointsNumber );
                List<Vector3> backwardCenterNewLeft = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ nl1, cb, c1 }, this.curvePointsNumber );

                switchFloor.centerEntranceLeft = forwardCenterNewLeft;
                switchFloor.centerExitLeft = backwardCenterNewLeft;

                nextStartingPoints.Add( nl1 );

                nextStartingDirections.Add( nl1 - cb );
            }
        }

        section.switchLights = switchLights;

        section.nextStartingDirections = nextStartingDirections; 
        section.nextStartingPoints = nextStartingPoints;
        section.floorPoints = switchFloor;

        section.activeSwitch = SwitchDirection.CenterToCenter;
        section.curvePointsCount = switchFloor.centerLine.Count;
        section.floorPoints = switchFloor;

        TrainController.UpdateSwitchLight( section );

        return section;
    }

    public LineSection GenerateBiToNewBiSwitch( LineSection switchSection, Vector3 startingDir, Vector3 startingPoint, GameObject sectionGameObj ) {

        LineSection section = new();
        section.type = Type.Switch;
        section.switchType = SwitchType.BiToNewBi;
        section.bidirectional =  true;
        section.newBidirectional =  true;

        MeshGenerator.Floor switchFloor = new();
        Vector3 switchDir = startingDir.normalized;

        Dictionary<SwitchDirection, List<GameObject>> switchLights = new();
        Vector3 c0, c1, r0, rb0, nr0, l0, lb0, nl0, r1, rb1, nr1, l1, lb1, nl1;
        float dw = ( this.centerWidth / 2 ) + ( this.tunnelWidth / 2 );
        float dl = ( this.switchLenght - this.tunnelWidth - this.centerWidth ) / 2;
        List<Vector3> nextStartingPoints = new();
        List<Vector3> nextStartingDirections = new();

        c0 = startingPoint;

        r0 = c0 + Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * dw;
        rb0 = r0 + ( switchDir * dl );
        rb1 = r0 + ( switchDir * ( this.switchLenght - dl ) );
        r1 = r0 + ( switchDir * this.switchLenght );

        l0 = c0 + Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * dw;
        lb0 = l0 + ( switchDir * dl );
        lb1 = l0 + ( switchDir * ( this.switchLenght - dl ) );
        l1 = l0 + ( switchDir * this.switchLenght );

        c1 = c0 + ( switchDir * this.switchLenght );

        nextStartingPoints.Add( c1 );
        nextStartingDirections.Add( startingDir );

        switchFloor.leftLine = new List<Vector3>{ l0, lb0, lb1, l1 };
        switchFloor.centerLine = new List<Vector3>{ c0, c1 };
        switchFloor.rightLine = new List<Vector3>{ r0, rb0, rb1, r1 };

        foreach( Side newSide in switchSection.newLinesStarts.Keys ) {

            if( newSide == Side.Right ) { // Nuova linea a Destra
                
                Vector3 ncr = switchSection.newLinesStarts[ newSide ].pos; 
                nr0 = ncr - switchDir * ( ( tunnelWidth / 2 ) + ( centerWidth / 2 ) );
                nr1 = ncr + switchDir * ( ( tunnelWidth / 2 ) + ( centerWidth / 2 ) );

                GameObject lightR0 = Instantiate( this.switchLight, r0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightR0.transform.parent = sectionGameObj.transform;
                lightR0.name = "Semaforo R0";
                GameObject lightR1 = Instantiate( this.switchLight, r1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightR1.transform.parent = sectionGameObj.transform;
                lightR1.name = "Semaforo R1";
                GameObject lightNR0 = Instantiate( this.switchLight, nr0 + ( Quaternion.Euler( 0.0f, 0.0f, 0.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( switchSection.newLinesStarts[ newSide ].dir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightNR0.transform.parent = sectionGameObj.transform;
                lightNR0.name = "Semaforo NR0";
                GameObject lightNR1 = Instantiate( this.switchLight, nr1 + ( Quaternion.Euler( 0.0f, 0.0f, 0.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( switchSection.newLinesStarts[ newSide ].dir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightNR1.transform.parent = sectionGameObj.transform;
                lightNR1.name = "Semaforo NR1";

                switchLights.Add( SwitchDirection.RightToRight, new List<GameObject>{ lightR0, lightR1 } );
                switchLights.Add( SwitchDirection.RightToEntranceRight, new List<GameObject>{ lightR0, lightNR0 } );
                switchLights.Add( SwitchDirection.RightToExitRight, new List<GameObject>{ lightNR1, lightR1 } );

                switchFloor.rightEntranceRight = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ r0, rb0, nr0 }, this.curvePointsNumber );
                switchFloor.rightExitRight = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ nr1, rb1, r1 }, this.curvePointsNumber );

                nextStartingPoints.Add( ncr );
                nextStartingDirections.Add( nr0 - rb0 );
            }
            else if( newSide == Side.Left ) { // Nuova linea a Sinistra

                Vector3 ncl = switchSection.newLinesStarts[ newSide ].pos; 
                nl0 = ncl - switchDir * ( ( tunnelWidth / 2 ) + ( centerWidth / 2 ) );
                nl1 = ncl + switchDir * ( ( tunnelWidth / 2 ) + ( centerWidth / 2 ) );

                GameObject lightL0 = Instantiate( this.switchLight, l0 + ( Quaternion.Euler( 0.0f, 0.0f, -90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) - 180.0f, this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightL0.transform.parent = sectionGameObj.transform;
                lightL0.name = "Semaforo L0";
                GameObject lightL1 = Instantiate( this.switchLight, l1 + ( Quaternion.Euler( 0.0f, 0.0f, 90.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightL1.transform.parent = sectionGameObj.transform;
                lightL1.name = "Semaforo L1";
                GameObject lightNL0 = Instantiate( this.switchLight, nl0 + ( Quaternion.Euler( 0.0f, 0.0f, 0.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) + Vector3.SignedAngle( switchSection.newLinesStarts[ newSide ].dir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightNL0.transform.parent = sectionGameObj.transform;
                lightNL0.name = "Semaforo NL0";
                GameObject lightNL1 = Instantiate( this.switchLight, nl1 + ( Quaternion.Euler( 0.0f, 0.0f, 0.0f ) * switchDir * this.switchLightDistance ) - Vector3.forward * this.switchLightHeight, Quaternion.Euler( this.switchLightRotation.x + Vector3.SignedAngle( startingDir, Vector3.right, -Vector3.forward ) + Vector3.SignedAngle( switchSection.newLinesStarts[ newSide ].dir, Vector3.right, -Vector3.forward ), this.switchLightRotation.y, this.switchLightRotation.z ) );
                lightNL1.transform.parent = sectionGameObj.transform;
                lightNL1.name = "Semaforo NL1";

                switchLights.Add( SwitchDirection.LeftToLeft, new List<GameObject>{ lightL0, lightL1 } );
                switchLights.Add( SwitchDirection.LeftToEntranceLeft, new List<GameObject>{ lightL0, lightNL0 } );
                switchLights.Add( SwitchDirection.LeftToExitLeft, new List<GameObject>{ lightNL1, lightL1 } );

                switchFloor.leftEntranceLeft = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ l0, lb0, nl0 }, this.curvePointsNumber );
                switchFloor.leftExitLeft = BezierCurveCalculator.CalculateBezierCurve( new List<Vector3>{ nl1, lb1, l1 }, this.curvePointsNumber );

                nextStartingPoints.Add( ncl );
                nextStartingDirections.Add( nl0 - lb0 );
            }
        }

        section.switchLights = switchLights;

        section.nextStartingDirections = nextStartingDirections; 
        section.nextStartingPoints = nextStartingPoints;
        section.floorPoints = switchFloor;

        if( switchSection.newLinesStarts.ContainsKey( Side.Right ) ) {
            section.activeSwitch = SwitchDirection.RightToRight;
        }
        else if( switchSection.newLinesStarts.ContainsKey( Side.Left ) ) {
            section.activeSwitch = SwitchDirection.LeftToLeft;
        }
        section.curvePointsCount = switchFloor.rightLine.Count;
        section.floorPoints = switchFloor;

        TrainController.UpdateSwitchLight( section );

        return section;
    }
}
