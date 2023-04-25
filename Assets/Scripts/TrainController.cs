using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    private enum Direction
    {
        None,
        Forward,
        Backward,
    }

    private enum Side
    {
        Center,
        Right,
        Left,
    }

    public float speed = 0.0f;
    public float maxSpeed = 250.0f;
    public float acceleration = 50.0f;
    public float deceleration = 75.0f;
    public float drag = 5.0f;

    public float deadZoneTriggerRight = 0.1f;
    public float deadZoneTriggerLeft = 0.1f;

    private Dictionary<string, List<LineSection>> lines;
    private AudioSource noise;
    private AudioSource braking;

    private bool goFowardActive = false;
    private bool goBackwardActive = false;
    private float brakingNoiseDecreasingSpeed = 2.0f;
    private float previousSpeed = 0.0f;
    private Direction mainDir = Direction.None;

    private string keyLine = "Linea 1";
    private int indexPoint = 0;
    private int indexSection = 0;

    private float heightCorrection;

    private Side railSide = Side.Right;
    private Direction actualMovement = Direction.None;

    private List<Vector3> actualSectionPoints = new List<Vector3>{ Vector3.zero };
    private Vector3 actualPoint = Vector3.zero;
    private Vector3 actualOrientationPoint = Vector3.zero;
    private int actualSwitchIndex = -1;

    private bool inverseSection = false;

    void Start()
    {
        AudioSource[] audios = this.transform.GetComponents<AudioSource>();
        foreach( AudioSource audio in audios ) {
            if( audio.clip.name.Equals( "Train Noise" ) ) {
                noise = audio;
                noise.Stop();
                noise.loop = true;
            }
            else if( audio.clip.name.Equals( "Train Brakes" ) ) {
                braking = audio;
                braking.Stop();
                braking.loop = true;
            }
        }

        FullLineGenerator lineGen = GameObject.Find( "LineGenerator" ).GetComponent<FullLineGenerator>();
        lines = lineGen.lineMap;
        heightCorrection = lineGen.trainHeightFromGround;

        Vector3 startPos = Vector3.zero;
        Vector3 startDir = Vector3.zero;
        MeshGenerator.Floor startingFloor = lines[ keyLine ][ indexSection ].floorPoints; 
        if( lineGen.startingBidirectional ) {
            if( Random.Range( 0, 2 ) == 0 ) {
                railSide = Side.Left;
                startPos = startingFloor.leftLine[ indexPoint ];
                startDir = startingFloor.leftLine[ indexPoint + 1 ] - startPos;
            }
            else {
                railSide = Side.Right;
                startPos = startingFloor.rightLine[ indexPoint ];
                startDir = startingFloor.rightLine[ indexPoint + 1 ] - startPos;
            }
        }
        else{ 
            railSide = Side.Center;
            startPos = startingFloor.centerLine[ indexPoint ];
            startDir = startingFloor.centerLine[ indexPoint + 1 ] - startPos;
        }
        this.transform.position = startPos + ( Vector3.forward * heightCorrection );
        this.transform.right = startDir;
    }

    // Update is called once per frame
    void Update()
    {
        previousSpeed = speed;
        HandleMovement();
        HandleBrakingNoise();
        HandleSwitch();
    }

    private void HandleSwitch() {

        if( actualMovement == Direction.Forward && actualSwitchIndex < indexSection ) {

            Debug.Log( "Calcolo switch Successivo" );

            List<LineSection> sections = lines[ keyLine ];

            actualSwitchIndex = sections.Count;
            for( int i = indexSection; i < sections.Count; i++ ) {
                if( sections[ i ].type == Type.Switch ) {
                    actualSwitchIndex = i;
                    break;
                }
            }

            Debug.Log( "actualSwitchIndex: " + actualSwitchIndex );
        }
        else if( actualMovement == Direction.Backward && actualSwitchIndex > indexSection ) {

            Debug.Log( "Calcolo switch Precedente" );

            List<LineSection> sections = lines[ keyLine ];

            actualSwitchIndex = -1;
            for( int i = indexSection; i >= 0; i-- ) {
                if( sections[ i ].type == Type.Switch ) {
                    actualSwitchIndex = i;
                    break;
                }
            }

            Debug.Log( "actualSwitchIndex: " + actualSwitchIndex );
        }

        if( ( Input.GetButtonDown( "RB" ) || Input.GetKeyDown( KeyCode.F ) ) && indexSection != actualSwitchIndex && actualSwitchIndex > -1 && actualSwitchIndex < lines[ keyLine ].Count ) {
            List<LineSection> sections = lines[ keyLine ];

            SwitchDirection selectedDir = new SwitchDirection();
            int sectionCount = -1;

            if( sections[ actualSwitchIndex ].switchType == SwitchType.BiToBi ) {
                if( actualMovement == Direction.Forward && railSide == Side.Right ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.RightToRight:  selectedDir = SwitchDirection.RightToLeft;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.rightLeftLine.Count;
                                                            break;
                        default:                            selectedDir = SwitchDirection.RightToRight;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.rightLine.Count;
                                                            break;
                    }
                }
                else if( actualMovement == Direction.Forward && railSide == Side.Left ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.LeftToLeft:    selectedDir = SwitchDirection.LeftToRight;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.leftRightLine.Count;
                                                            break;
                        default:                            selectedDir = SwitchDirection.LeftToLeft;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.leftLine.Count;
                                                            break;
                    }
                }
                else if( actualMovement == Direction.Backward && railSide == Side.Right ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.RightToRight:  selectedDir = SwitchDirection.LeftToRight;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.leftRightLine.Count;
                                                            break;
                        default:                            selectedDir = SwitchDirection.RightToRight;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.rightLine.Count;
                                                            break;
                    }
                }
                else if( actualMovement == Direction.Backward && railSide == Side.Left ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.LeftToLeft:    selectedDir = SwitchDirection.RightToLeft;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.rightLeftLine.Count;
                                                            break;
                        default:                            selectedDir = SwitchDirection.LeftToLeft;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.leftLine.Count;
                                                            break;
                    }
                }
            }
            else if( sections[ actualSwitchIndex ].switchType == SwitchType.BiToMono || sections[ actualSwitchIndex ].switchType == SwitchType.MonoToBi ) {
                if( railSide == Side.Center ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.RightToCenter: selectedDir = SwitchDirection.LeftToCenter;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.leftCenterLine.Count;
                                                            break;
                        case SwitchDirection.LeftToCenter:  selectedDir = SwitchDirection.RightToCenter;
                                                            sectionCount = sections[ actualSwitchIndex ].floorPoints.rightCenterLine.Count;
                                                            break;
                    }
                }
                else if( railSide == Side.Right ) {
                    selectedDir = SwitchDirection.RightToCenter;
                    sectionCount = sections[ actualSwitchIndex ].floorPoints.rightCenterLine.Count;                              
                }
                else if( railSide == Side.Left ) {
                    selectedDir = SwitchDirection.LeftToCenter;
                    sectionCount = sections[ actualSwitchIndex ].floorPoints.leftCenterLine.Count;                              
                }
            }
            else if( sections[ actualSwitchIndex ].switchType == SwitchType.MonoToNewMono ) {

                if( actualMovement == Direction.Forward ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.CenterToCenter:    selectedDir = SwitchDirection.CenterToNewLineForward;
                                                                sectionCount = sections[ actualSwitchIndex ].floorPoints.leftCenterNewLine.Count;
                                                                break;
                        default:                                selectedDir = SwitchDirection.CenterToCenter;
                                                                sectionCount = sections[ actualSwitchIndex ].floorPoints.centerLine.Count;
                                                                break;
                    }
                }
                else if( actualMovement == Direction.Backward ) {
                    switch( sections[ actualSwitchIndex ].activeSwitch ) {
                        case SwitchDirection.CenterToCenter:    selectedDir = SwitchDirection.CenterToNewLineBackward;
                                                                sectionCount = sections[ actualSwitchIndex ].floorPoints.rightCenterNewLine.Count;
                                                                break;
                        default:                                selectedDir = SwitchDirection.CenterToCenter;
                                                                sectionCount = sections[ actualSwitchIndex ].floorPoints.centerLine.Count;
                                                                break;
                    }                        
                }
            }

            sections[ actualSwitchIndex ].activeSwitch = selectedDir;
            sections[ actualSwitchIndex ].curvePointsCount = sectionCount;

            Debug.Log( "Scambio sezione " + actualSwitchIndex + " selezionato: " + sections[ actualSwitchIndex ].activeSwitch );

            Dictionary<string, List<Light>> updateLast = new Dictionary<string, List<Light>>();
            foreach( SwitchDirection activeSwitch in sections[ actualSwitchIndex ].switchLights.Keys ) {
                List<GameObject> lights = sections[ actualSwitchIndex ].switchLights[ activeSwitch ];
                foreach( GameObject light in lights ) {
                    foreach( Transform child in light.transform ) {
                        if( activeSwitch != sections[ actualSwitchIndex ].activeSwitch ) {
                            if( child.name == "Red Light" ) {
                                child.GetComponent<Light>().intensity = 15.0f;
                            }
                            else if( child.name == "Green Light" ) {
                                child.GetComponent<Light>().intensity = 0.0f;
                            }
                        }
                        else {
                            if( child.name == "Red Light" ) {
                                if( updateLast.ContainsKey( "OFF" ) ){
                                    updateLast[ "OFF" ].Add( child.GetComponent<Light>() );
                                }
                                else {
                                    updateLast.Add( "OFF", new List<Light>{ child.GetComponent<Light>() } );
                                }
                                
                                //child.GetComponent<Light>().intensity = 1.0f;
                            }
                            else if( child.name == "Green Light" ) {
                                if( updateLast.ContainsKey( "ON" ) ){
                                    updateLast[ "ON" ].Add( child.GetComponent<Light>() );
                                }
                                else {
                                    updateLast.Add( "ON", new List<Light>{ child.GetComponent<Light>() } );
                                }
                                //child.GetComponent<Light>().intensity = 0.0f;
                            }
                        }
                    }
                }
            }

            foreach( string status in  updateLast.Keys ) {
                if( status == "OFF" ) {
                    foreach( Light switchLight in updateLast[ "OFF" ] ) {
                        switchLight.GetComponent<Light>().intensity = 0.0f;

                    }
                }
                else if( status == "ON" ) {
                    foreach( Light switchLight in updateLast[ "ON" ] ) {
                        switchLight.GetComponent<Light>().intensity = 15.0f;
                    }
                }
            }
        }
    }

    private void HandleMovement() {
        if( speed > 1.0f && !goFowardActive )
        {
            noise.Play();
            StartCoroutine( "GoForward" );
            goFowardActive = true;
        }
        else if( speed <= 1.0f && goFowardActive) {
            noise.Stop();
            StopCoroutine( "GoForward" );
            goFowardActive = false;
        }

        if( speed < -1.0f && !goBackwardActive )
        {
            noise.Play();
            StartCoroutine( "GoBackward" );
            goBackwardActive = true;
        }
        else if( speed >= -1.0f && goBackwardActive) {
            noise.Stop();
            StopCoroutine( "GoBackward" );
            goBackwardActive = false;
        }

        if( Input.GetKey( KeyCode.D ) || Input.GetAxis( "RT" ) > ( deadZoneTriggerRight ) ) {

            float rightTriggerPression = 1.0f;
            if( Input.GetAxis( "RT" ) > ( deadZoneTriggerRight ) ) {
                rightTriggerPression = Input.GetAxis( "RT" );
            }

            //Debug.Log( "rightTriggerPression: " + rightTriggerPression );

            if( mainDir == Direction.Forward || mainDir == Direction.None ) {
                speed += acceleration * rightTriggerPression * Time.deltaTime;
            }
            else if( mainDir == Direction.Backward ) {
                // In questo caso la speed è negativa
                speed += deceleration * Time.deltaTime;
            }

            if( speed > maxSpeed ) {
                speed = maxSpeed;
            }
        }
        if( Input.GetKey( KeyCode.A ) || Input.GetAxis( "LT" ) > ( deadZoneTriggerLeft ) ) {

            float leftTriggerPression = 1.0f;
            if( Input.GetAxis( "LT" ) > ( deadZoneTriggerLeft ) ) {
                leftTriggerPression = Input.GetAxis( "LT" );
            }

            if( mainDir == Direction.Backward || mainDir == Direction.None ) {
                speed -= acceleration * leftTriggerPression * Time.deltaTime;
            }
            else if( mainDir == Direction.Forward ) {
                // In questo caso la speed è positiva
                speed -= deceleration * Time.deltaTime;
            }

            if( speed < -maxSpeed ) {
                speed = -maxSpeed;
            }
        }

        if( Mathf.Abs( speed ) >= acceleration * Time.deltaTime ) {
            if( mainDir == Direction.Forward ) {
                speed -= drag * Time.deltaTime;
            }
            else if( mainDir == Direction.Backward ) {
                speed += drag * Time.deltaTime;
            }
        }
        else if( Mathf.Abs( speed ) <= acceleration * Time.deltaTime ) {
            speed = 0.0f;
        }

        noise.pitch = Mathf.Abs( speed ) / maxSpeed;
    }

    private void HandleBrakingNoise() {

        if( ( ( Input.GetKey( KeyCode.D ) || Input.GetAxis( "RT" ) > ( deadZoneTriggerRight ) ) && mainDir == Direction.Backward ) || ( ( Input.GetKey( KeyCode.A ) || Input.GetAxis( "LT" ) > ( deadZoneTriggerLeft ) ) && mainDir == Direction.Forward ) ) { 

            //Debug.Log( "Braking" );
            if( !braking.isPlaying ) {
                braking.volume = Mathf.Abs( speed ) / maxSpeed;
                braking.time = 0.0f;
                braking.Play();
            }
            else {
                braking.volume = Mathf.Lerp( 0.0f, 1.0f, ( Mathf.Abs( speed ) / maxSpeed ) );
            }
        }
        else{
            braking.volume = Mathf.Lerp( braking.volume, 0.0f, brakingNoiseDecreasingSpeed * Time.deltaTime );

            if( braking.isPlaying && braking.volume <= 0.0f ) {
                //Debug.Log( "Stop Braking" );
                braking.Stop();
            }
        }
    }

    private List<Vector3> getPoints( LineSection section ) {
        Debug.Log( ">>> Bidirectional: " + section.bidirectional );
        Debug.Log( ">>> Type:          " + section.type );

        List<Vector3> points = null;
        if( section.type == Type.Switch ) {
            switch ( section.activeSwitch ) {
                case SwitchDirection.RightToRight:              points = section.floorPoints.rightLine; 
                                                                break;
                case SwitchDirection.RightToLeft:               points = section.floorPoints.rightLeftLine; 
                                                                break;
                case SwitchDirection.LeftToLeft:                points = section.floorPoints.leftLine; 
                                                                break;
                case SwitchDirection.LeftToRight:               points = section.floorPoints.leftRightLine; 
                                                                break;
                case SwitchDirection.RightToCenter:             points = section.floorPoints.rightCenterLine; 
                                                                break;
                case SwitchDirection.LeftToCenter:              points = section.floorPoints.leftCenterLine; 
                                                                break; 
                case SwitchDirection.CenterToNewLineForward:    points = section.floorPoints.leftCenterNewLine;
                                                                break; 
                case SwitchDirection.CenterToNewLineBackward:   points = section.floorPoints.rightCenterNewLine; 
                                                                break;
                case SwitchDirection.CenterToCenter:            points = section.floorPoints.centerLine; 
                                                                break;
            }
        }
        else {
            if( section.bidirectional ) {
            
                if( railSide == Side.Left ) {
                    points = section.floorPoints.leftLine;
                }
                else if( railSide == Side.Right ) {
                    points = section.floorPoints.rightLine;
                }
            }
            else {
                points = section.floorPoints.centerLine;
            }
        }

        return points;
    }
    
    IEnumerator GoForward()
    {
        actualMovement = Direction.Forward;
        //foreach( string lineName in lines.Keys ) {
            List<LineSection> sections = lines[ keyLine ];

            // Ciclo le sezioni della linea in avanti
            for( int i = indexSection; i < sections.Count; i++ ) {

                if( !inverseSection ) {
                
                    List<Vector3> points = getPoints( sections[ i ] );
                    actualSectionPoints = points;
                    
                    float deltaDist = 0.0f;
                    float dist = 0.0f;
                    // Finché non sono abbastanza vicino all'ultimo punto della curva (mi sto muovendo in avanti, quindi "navigo" la lista dei punti in avanti)
                    // continuo a cercare il punto sufficientemente lontano dal vagone per raggiungerlo
                    while( ( points[ points.Count - 1 ] - this.transform.position ).magnitude > deltaDist ) {
                        // Distanza che sarà percorsa in un frame
                        deltaDist = Time.deltaTime * Mathf.Abs( speed );
                        Vector3 nextPoint = Vector3.zero;
                        Vector3 nextOrientationPoint = Vector3.zero;

                        // Aggiornamento del movimento attuale del vagone (in questo caso avanti)
                        if( mainDir == Direction.Backward ) {

                            // Se il vagone stava andando indietro, allora devo puntare all'index successivo, ma se sono
                            // già all'ultimo punto della curva passo alla sezione succcessiva
                            if( indexPoint + 1 < points.Count ) {
                                indexPoint++;
                            }
                        }
                        mainDir = Direction.Forward;

                        int startingIndex = indexPoint;
                        // Ricerca del punto della curva successivo più lontano della distanza (per frame) percorsa dal vagone, che diventerà
                        // il punto successivo verso cui si dirigerà il vagone
                        for( int j = startingIndex; j < points.Count; j++ ) {
                            int indexDiff = j - startingIndex;
                            Debug.Log( "j: " + j );
                            dist = ( ( points[ j ] + ( Vector3.forward * heightCorrection ) ) - this.transform.position ).magnitude;
                            Debug.Log( "dist: " + dist );

                            if( j + indexDiff >= points.Count ) {
                                Debug.Log( "Cerco punto in sezione successiva" );
                                if( i + 1 < sections.Count && sections[ i ].type != Type.Switch && sections[ i + 1 ].type != Type.Switch ) {
                                    indexDiff = j + indexDiff - ( points.Count - 1 );

                                    //if( sections[ i + 1 ].type == Type.Tunnel ) {
                                        if( sections[ i + 1 ].bidirectional ) {
                                            if( railSide == Side.Left ) {
                                                nextOrientationPoint = sections[ i + 1 ].floorPoints.leftLine[ indexDiff ]  + ( Vector3.forward * heightCorrection );
                                            }
                                            else if( railSide == Side.Right ) {
                                                nextOrientationPoint = sections[ i + 1 ].floorPoints.rightLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                            }
                                        }
                                        else {
                                            nextOrientationPoint = sections[ i + 1 ].floorPoints.centerLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                        }
                                    //}
                                    //else {
                                        //nextOrientationPoint = sections[ i + 1 ].bezierCurveLimitedAngle[ indexDiff ];
                                    //}
                                }
                                else {
                                    nextOrientationPoint = points[ points.Count - 1 ] + ( Vector3.forward * heightCorrection );
                                }
                            }
                            else {
                                nextOrientationPoint = points[ j + indexDiff ] + ( Vector3.forward * heightCorrection );
                            }

                            if( dist > deltaDist ) {
                                indexPoint = j;
                                nextPoint = points[ j ] + ( Vector3.forward * heightCorrection );
                                Debug.Log( "Indice nextPoint trovato: " + indexPoint );
                                break;
                            }
                        }

                        if( nextPoint == Vector3.zero ) {
                            //Debug.Log( "Sezione terminata" );
                            break;
                        }

                        actualPoint = nextPoint;
                        actualOrientationPoint = nextOrientationPoint;
                        
                        // Posizione e rotazione iniziale del vagone per punto della curva
                        Vector3 nextDir = nextOrientationPoint - this.transform.position;
                        Vector3 startDir = this.transform.right;
                        Vector3 startPos = this.transform.position;
                        float segmentDist = ( nextPoint - startPos ).magnitude;
                        float sumDist = 0.0f;
                        while( sumDist < segmentDist ) {
                            Debug.DrawLine( this.transform.position, nextPoint, Color.red, Time.deltaTime );
                            Debug.DrawLine( this.transform.position, nextOrientationPoint, Color.magenta, Time.deltaTime );

                            // Interpolazione lineare (sulla distanza) per gestire posizione e rotazione frame successivo
                            this.transform.position = Vector3.Lerp( startPos, nextPoint, sumDist / dist ); 
                            this.transform.right = Vector3.Slerp( startDir, nextDir, sumDist / dist );

                            sumDist += Time.deltaTime * Mathf.Abs( speed );

                            if( sumDist < segmentDist ) {
                                yield return null;
                            }
                            else{
                                //Debug.Log( "Next Point raggiunto" );
                                break;
                            }
                        }

                        //Debug.Log( "While segmento terminato" );
                    }

                    if( sections[ i ].type == Type.Switch ) {
                        if( sections[ i ].switchType == SwitchType.BiToBi ) {
                            if( railSide == Side.Right && sections[ i ].activeSwitch == SwitchDirection.RightToLeft ) {
                                railSide = Side.Left;
                            }
                            else if( railSide == Side.Left && sections[ i ].activeSwitch == SwitchDirection.LeftToRight ) {
                                railSide = Side.Right;
                            }
                        }
                        else if( sections[ i ].switchType == SwitchType.BiToMono ) {
                            railSide = Side.Center;
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToNewMono ) {
                            railSide = Side.Center;
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToBi ) {
                            if( sections[ i ].activeSwitch == SwitchDirection.LeftToCenter ) {
                                railSide = Side.Left;
                            }
                            else if( sections[ i ].activeSwitch == SwitchDirection.RightToCenter ) {
                                railSide = Side.Right;
                            }
                        }
                    }
                }
                else {
                    List<Vector3> points = getPoints( sections[ i ] );
                    actualSectionPoints = points;
                    
                    float deltaDist = 0.0f;
                    float dist = 0.0f;
                    // Finché non sono abbastanza vicino al primo punto della curva (mi sto muovendo all'indietro, quindi "navigo" la lista dei punti all'indietro)
                    // continuo a cercare il punto sufficientemente lontano dal vagone per raggiungerlo
                    while( ( points[ 0 ] - this.transform.position ).magnitude > deltaDist ) {
                        // Distanza che sarà percorsa in un frame
                        deltaDist = Time.deltaTime * Mathf.Abs( speed );
                        Vector3 previousPoint = Vector3.zero;
                        Vector3 previousOrientationPoint = Vector3.zero;

                        // Aggiornamento del movimento attuale del vagone (in questo caso indietro)
                        if( mainDir == Direction.Backward ) {
                            // Se il vagone stava andando avanti, allora devo puntare all'index precedente, ma se sono
                            // già al primo punto della curva passo alla sezione precedente
                            if( indexPoint - 1 >= 0 ) {
                                indexPoint--;
                            }
                        }
                        mainDir = Direction.Forward;

                        int startingIndex = indexPoint;
                        // Ricerca del punto della curva precedente più lontano della distanza (per frame) percorsa dal vagone, che diventerà
                        // il punto successivo verso cui si dirigerà il vagone
                        for( int j = indexPoint; j >= 0; j-- ) {
                            int indexDiff = startingIndex - j;
                            dist = ( ( points[ j ] + ( Vector3.forward * heightCorrection ) ) - this.transform.position ).magnitude;

                            if( j - indexDiff < 0 ) {
                                if( i - 1 >= 0 && sections[ i ].type != Type.Switch && sections[ i - 1 ].type != Type.Switch ) {
                                    //indexDiff = ( sections[ i - 1 ].bezierCurveLimitedAngle.Count - 1 ) + ( j - indexDiff );

                                    //if( sections[ i - 1 ].type == Type.Tunnel ) {
                                        if( sections[ i - 1 ].bidirectional ) {
                                            if( railSide == Side.Left ) {
                                                indexDiff = ( sections[ i - 1 ].floorPoints.leftLine.Count - 1 ) + ( j - indexDiff );
                                                previousOrientationPoint = sections[ i - 1 ].floorPoints.leftLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                            }
                                            else if( railSide == Side.Right ) {
                                                indexDiff = ( sections[ i - 1 ].floorPoints.rightLine.Count - 1 ) + ( j - indexDiff );
                                                previousOrientationPoint = sections[ i - 1 ].floorPoints.rightLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                            }
                                            
                                        }
                                        else {
                                            indexDiff = ( sections[ i - 1 ].floorPoints.centerLine.Count - 1 ) + ( j - indexDiff );
                                            previousOrientationPoint = sections[ i - 1 ].floorPoints.centerLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                        }
                                    //}
                                    //else {
                                        //previousOrientationPoint = sections[ i - 1 ].bezierCurveLimitedAngle[ indexDiff ];
                                    //}
                                }
                                else {
                                    previousOrientationPoint = points[ 0 ] + ( Vector3.forward * heightCorrection );
                                }
                            }
                            else {
                                previousOrientationPoint = points[ j - indexDiff ] + ( Vector3.forward * heightCorrection );
                            }

                            if( dist > deltaDist ) {
                                indexPoint = j;
                                previousPoint = points[ j ] + ( Vector3.forward * heightCorrection );
                                break;
                            }
                        }
                        
                        if( previousPoint == Vector3.zero ) {
                            break;
                        }

                        actualPoint = previousPoint;
                        actualOrientationPoint = previousOrientationPoint;

                        // Posizione e rotazione iniziale del vagone per punto della curva
                        Vector3 previousDir = previousOrientationPoint - this.transform.position;
                        Vector3 startDir = this.transform.right;
                        Vector3 startPos = this.transform.position;
                        float segmentDist = ( previousPoint - startPos ).magnitude;
                        float sumDist = 0.0f;
                        while( sumDist < segmentDist ) {

                            Debug.DrawLine( this.transform.position, previousPoint, Color.red, Time.deltaTime );
                            Debug.DrawLine( this.transform.position, previousOrientationPoint, Color.magenta, Time.deltaTime );

                            // Interpolazione lineare (sulla distanza) per gestire posizione e rotazione frame successivo
                            this.transform.position = Vector3.Lerp( startPos, previousPoint, sumDist / dist ); 
                            this.transform.right = Vector3.Slerp( startDir, previousDir, sumDist / dist );

                            sumDist += Time.deltaTime * Mathf.Abs( speed );

                            if( sumDist < segmentDist ) {
                                yield return null;
                            }
                            else{
                                //Debug.Log( "Next Point raggiunto" );
                                break;
                            }
                        }
                    }

                    if( sections[ i ].type == Type.Switch ) {
                        if( sections[ i ].switchType == SwitchType.BiToBi ) {
                            if( railSide == Side.Right && sections[ i ].activeSwitch == SwitchDirection.LeftToRight ) {
                            railSide = Side.Left;
                            }
                            else if( railSide == Side.Left && sections[ i ].activeSwitch == SwitchDirection.RightToLeft ) {
                                railSide = Side.Right;
                            }
                        }
                        else if( sections[ i ].switchType == SwitchType.BiToMono ) {
                            if( sections[ i ].activeSwitch == SwitchDirection.LeftToCenter ) {
                                railSide = Side.Left;
                            }
                            else if( sections[ i ].activeSwitch == SwitchDirection.RightToCenter ) {
                                railSide = Side.Right;
                            }
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToBi ) {
                            railSide = Side.Center;
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToNewMono ) {
                            railSide = Side.Center;
                        }
                    }
                }

                Debug.Log( "railSide: " + railSide );

                if( i < sections.Count ) {
                    indexSection++;
                    List<Vector3> nextPoints = getPoints( sections[ indexSection ] );

                    // Movimento forward: il primo punto della sezione successiva più vicino deve essere quello con indice 0, altrimenti devo ciclare la lista dei punti al contrario
                    if( ( this.transform.position - nextPoints[ 0 ] ).magnitude <= ( this.transform.position - nextPoints[ nextPoints.Count - 1 ] ).magnitude ) {
                        inverseSection = false;
                        indexPoint = 0;
                    }
                    else {
                        inverseSection = true;
                        indexPoint = sections[ indexSection ].curvePointsCount - 1;
                    }

                    Debug.Log( ">>>>>>> inverseSection: " + inverseSection );
                    Debug.Log( ">>>>>>> Previous indexPoint: " + indexPoint );
                }
            }
        //}

        //Debug.Log( "GoFoward ended" );
    }

    IEnumerator GoBackward()
    {
        actualMovement = Direction.Backward;
        //foreach( string lineName in lines.Keys ) {
            List<LineSection> sections = lines[ keyLine ];

            // Ciclo le sezioni della linea all'indietro
            for( int i = indexSection; i >= 0; i-- ) {

                if( !inverseSection ) {

                    List<Vector3> points = getPoints( sections[ i ] );
                    actualSectionPoints = points;
                    
                    float deltaDist = 0.0f;
                    float dist = 0.0f;
                    // Finché non sono abbastanza vicino al primo punto della curva (mi sto muovendo all'indietro, quindi "navigo" la lista dei punti all'indietro)
                    // continuo a cercare il punto sufficientemente lontano dal vagone per raggiungerlo
                    while( ( points[ 0 ] - this.transform.position ).magnitude > deltaDist ) {
                        // Distanza che sarà percorsa in un frame
                        deltaDist = Time.deltaTime * Mathf.Abs( speed );
                        Vector3 previousPoint = Vector3.zero;
                        Vector3 previousOrientationPoint = Vector3.zero;

                        // Aggiornamento del movimento attuale del vagone (in questo caso indietro)
                        if( mainDir == Direction.Forward ) {
                            // Se il vagone stava andando avanti, allora devo puntare all'index precedente, ma se sono
                            // già al primo punto della curva passo alla sezione precedente
                            if( indexPoint - 1 >= 0 ) {
                                indexPoint--;
                            }
                        }
                        mainDir = Direction.Backward;

                        int startingIndex = indexPoint;
                        // Ricerca del punto della curva precedente più lontano della distanza (per frame) percorsa dal vagone, che diventerà
                        // il punto successivo verso cui si dirigerà il vagone
                        for( int j = indexPoint; j >= 0; j-- ) {
                            int indexDiff = startingIndex - j;
                            dist = ( ( points[ j ] + ( Vector3.forward * heightCorrection ) ) - this.transform.position ).magnitude;

                            if( j - indexDiff < 0 ) {
                                if( i - 1 >= 0 && sections[ i ].type != Type.Switch && sections[ i - 1 ].type != Type.Switch ) {
                                    //indexDiff = ( sections[ i - 1 ].bezierCurveLimitedAngle.Count - 1 ) + ( j - indexDiff );

                                    //if( sections[ i - 1 ].type == Type.Tunnel ) {
                                        if( sections[ i - 1 ].bidirectional ) {
                                            if( railSide == Side.Left ) {
                                                indexDiff = ( sections[ i - 1 ].floorPoints.leftLine.Count - 1 ) + ( j - indexDiff );
                                                previousOrientationPoint = sections[ i - 1 ].floorPoints.leftLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                            }
                                            else if( railSide == Side.Right ) {
                                                indexDiff = ( sections[ i - 1 ].floorPoints.rightLine.Count - 1 ) + ( j - indexDiff );
                                                previousOrientationPoint = sections[ i - 1 ].floorPoints.rightLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                            }
                                            
                                        }
                                        else {
                                            indexDiff = ( sections[ i - 1 ].floorPoints.centerLine.Count - 1 ) + ( j - indexDiff );
                                            previousOrientationPoint = sections[ i - 1 ].floorPoints.centerLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                        }
                                    //}
                                    //else {
                                        //previousOrientationPoint = sections[ i - 1 ].bezierCurveLimitedAngle[ indexDiff ];
                                    //}
                                }
                                else {
                                    previousOrientationPoint = points[ 0 ] + ( Vector3.forward * heightCorrection );
                                }
                            }
                            else {
                                previousOrientationPoint = points[ j - indexDiff ] + ( Vector3.forward * heightCorrection );
                            }

                            if( dist > deltaDist ) {
                                indexPoint = j;
                                previousPoint = points[ j ] + ( Vector3.forward * heightCorrection );
                                break;
                            }
                        }
                        
                        if( previousPoint == Vector3.zero ) {
                            break;
                        }

                        actualPoint = previousPoint;
                        actualOrientationPoint = previousOrientationPoint;

                        // Posizione e rotazione iniziale del vagone per punto della curva
                        Vector3 previousDir = this.transform.position - previousOrientationPoint;
                        Vector3 startDir = this.transform.right;
                        Vector3 startPos = this.transform.position;
                        float segmentDist = ( previousPoint - startPos ).magnitude;
                        float sumDist = 0.0f;
                        while( sumDist < segmentDist ) {

                            Debug.DrawLine( this.transform.position, previousPoint, Color.red, Time.deltaTime );
                            Debug.DrawLine( this.transform.position, previousOrientationPoint, Color.magenta, Time.deltaTime );

                            // Interpolazione lineare (sulla distanza) per gestire posizione e rotazione frame successivo
                            this.transform.position = Vector3.Lerp( startPos, previousPoint, sumDist / dist ); 
                            this.transform.right = Vector3.Slerp( startDir, previousDir, sumDist / dist );

                            sumDist += Time.deltaTime * Mathf.Abs( speed );

                            if( sumDist < segmentDist ) {
                                yield return null;
                            }
                            else{
                                //Debug.Log( "Next Point raggiunto" );
                                break;
                            }
                        }
                    }

                    if( sections[ i ].type == Type.Switch ) {
                        if( sections[ i ].switchType == SwitchType.BiToBi ) {
                            if( railSide == Side.Right && sections[ i ].activeSwitch == SwitchDirection.LeftToRight ) {
                            railSide = Side.Left;
                            }
                            else if( railSide == Side.Left && sections[ i ].activeSwitch == SwitchDirection.RightToLeft ) {
                                railSide = Side.Right;
                            }
                        }
                        else if( sections[ i ].switchType == SwitchType.BiToMono ) {
                            if( sections[ i ].activeSwitch == SwitchDirection.LeftToCenter ) {
                                railSide = Side.Left;
                            }
                            else if( sections[ i ].activeSwitch == SwitchDirection.RightToCenter ) {
                                railSide = Side.Right;
                            }
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToBi ) {
                            railSide = Side.Center;
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToNewMono ) {
                            railSide = Side.Center;
                        }
                    }

                }
                else {
                    Debug.Log( ">>>>>>> Navigating inverse" );


                    List<Vector3> points = getPoints( sections[ i ] );
                    actualSectionPoints = points;
                    
                    float deltaDist = 0.0f;
                    float dist = 0.0f;
                    // Finché non sono abbastanza vicino all'ultimo punto della curva (mi sto muovendo in avanti, quindi "navigo" la lista dei punti in avanti)
                    // continuo a cercare il punto sufficientemente lontano dal vagone per raggiungerlo
                    while( ( points[ points.Count - 1 ] - this.transform.position ).magnitude > deltaDist ) {
                        // Distanza che sarà percorsa in un frame
                        deltaDist = Time.deltaTime * Mathf.Abs( speed );
                        Vector3 nextPoint = Vector3.zero;
                        Vector3 nextOrientationPoint = Vector3.zero;

                        // Aggiornamento del movimento attuale del vagone (in questo caso avanti)
                        if( mainDir == Direction.Forward ) {

                            // Se il vagone stava andando indietro, allora devo puntare all'index successivo, ma se sono
                            // già all'ultimo punto della curva passo alla sezione succcessiva
                            if( indexPoint + 1 < points.Count ) {
                                indexPoint++;
                            }
                        }
                        mainDir = Direction.Backward;

                        int startingIndex = indexPoint;
                        // Ricerca del punto della curva successivo più lontano della distanza (per frame) percorsa dal vagone, che diventerà
                        // il punto successivo verso cui si dirigerà il vagone
                        for( int j = startingIndex; j < points.Count; j++ ) {
                            int indexDiff = j - startingIndex;
                            Debug.Log( "j: " + j );
                            dist = ( ( points[ j ] + ( Vector3.forward * heightCorrection ) ) - this.transform.position ).magnitude;
                            Debug.Log( "dist: " + dist );

                            if( j + indexDiff >= points.Count ) {
                                Debug.Log( "Cerco punto in sezione successiva" );
                                if( i + 1 < sections.Count && sections[ i ].type != Type.Switch && sections[ i + 1 ].type != Type.Switch ) {
                                    indexDiff = j + indexDiff - ( points.Count - 1 );

                                    //if( sections[ i + 1 ].type == Type.Tunnel ) {
                                        if( sections[ i + 1 ].bidirectional ) {
                                            if( railSide == Side.Left ) {
                                                nextOrientationPoint = sections[ i + 1 ].floorPoints.leftLine[ indexDiff ]  + ( Vector3.forward * heightCorrection );
                                            }
                                            else if( railSide == Side.Right ) {
                                                nextOrientationPoint = sections[ i + 1 ].floorPoints.rightLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                            }
                                        }
                                        else {
                                            nextOrientationPoint = sections[ i + 1 ].floorPoints.centerLine[ indexDiff ] + ( Vector3.forward * heightCorrection );
                                        }
                                    //}
                                    //else {
                                        //nextOrientationPoint = sections[ i + 1 ].bezierCurveLimitedAngle[ indexDiff ];
                                    //}
                                }
                                else {
                                    nextOrientationPoint = points[ points.Count - 1 ] + ( Vector3.forward * heightCorrection );
                                }
                            }
                            else {
                                nextOrientationPoint = points[ j + indexDiff ] + ( Vector3.forward * heightCorrection );
                            }

                            if( dist > deltaDist ) {
                                indexPoint = j;
                                nextPoint = points[ j ] + ( Vector3.forward * heightCorrection );
                                Debug.Log( "Indice nextPoint trovato: " + indexPoint );
                                break;
                            }
                        }

                        if( nextPoint == Vector3.zero ) {
                            //Debug.Log( "Sezione terminata" );
                            break;
                        }

                        actualPoint = nextPoint;
                        actualOrientationPoint = nextOrientationPoint;
                        
                        // Posizione e rotazione iniziale del vagone per punto della curva
                        Vector3 nextDir = this.transform.position - nextOrientationPoint;
                        Vector3 startDir = this.transform.right;
                        Vector3 startPos = this.transform.position;
                        float segmentDist = ( nextPoint - startPos ).magnitude;
                        float sumDist = 0.0f;
                        while( sumDist < segmentDist ) {
                            Debug.DrawLine( this.transform.position, nextPoint, Color.red, Time.deltaTime );
                            Debug.DrawLine( this.transform.position, nextOrientationPoint, Color.magenta, Time.deltaTime );

                            // Interpolazione lineare (sulla distanza) per gestire posizione e rotazione frame successivo
                            this.transform.position = Vector3.Lerp( startPos, nextPoint, sumDist / dist ); 
                            this.transform.right = Vector3.Slerp( startDir, nextDir, sumDist / dist );

                            sumDist += Time.deltaTime * Mathf.Abs( speed );

                            if( sumDist < segmentDist ) {
                                yield return null;
                            }
                            else{
                                //Debug.Log( "Next Point raggiunto" );
                                break;
                            }
                        }

                        //Debug.Log( "While segmento terminato" );
                    }

                    if( sections[ i ].type == Type.Switch ) {
                        if( sections[ i ].switchType == SwitchType.BiToBi ) {
                            if( railSide == Side.Right && sections[ i ].activeSwitch == SwitchDirection.RightToLeft ) {
                                railSide = Side.Left;
                            }
                            else if( railSide == Side.Left && sections[ i ].activeSwitch == SwitchDirection.LeftToRight ) {
                                railSide = Side.Right;
                            }
                        }
                        else if( sections[ i ].switchType == SwitchType.BiToMono ) {
                            railSide = Side.Center;
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToNewMono ) {
                            railSide = Side.Center;
                        }
                        else if( sections[ i ].switchType == SwitchType.MonoToBi ) {
                            if( sections[ i ].activeSwitch == SwitchDirection.LeftToCenter ) {
                                railSide = Side.Left;
                            }
                            else if( sections[ i ].activeSwitch == SwitchDirection.RightToCenter ) {
                                railSide = Side.Right;
                            }
                        }
                    }
                }

                Debug.Log( "railSide: " + railSide );

                // Aggiornamento index una volta raggiunto il punto iniziale della curva
                if( i > 0 ) {
                    indexSection--;
                    List<Vector3> nextPoints = getPoints( sections[ indexSection ] );
                    // Movimento backward: il primo punto della sezione successiva più vicino deve essere quello con indice (Count - 1), altrimenti devo ciclare la lista dei punti al contrario
                    if( ( this.transform.position - nextPoints[ nextPoints.Count - 1 ] ).magnitude <= ( this.transform.position - nextPoints[ 0 ] ).magnitude ) {
                        inverseSection = false;
                        indexPoint = sections[ indexSection ].curvePointsCount - 1;
                    }
                    else {
                        inverseSection = true;
                        indexPoint = 0;
                    }

                    Debug.Log( ">>>>>>> inverseSection: " + inverseSection );
                    Debug.Log( ">>>>>>> Previous indexPoint: " + indexPoint );
                }
            }
        //}

        //Debug.Log( "GoFoward ended" );
    }

        private void OnDrawGizmos()
    {
        if( actualSectionPoints != null ) {
            for( int i = 1; i < actualSectionPoints.Count; i++ ) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere( actualSectionPoints[ i ], 0.5f );
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere( actualPoint, 1.0f );
            Gizmos.color = Color.red;
            Gizmos.DrawSphere( actualOrientationPoint, 0.75f );
        }
    }
}
