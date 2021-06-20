using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


/// <summary>
/// Beija Flor Machine Learning Agent
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent�s camera")]
    public Camera agentCamera;

    [Tooltip("Wheter this is training or gameplay mode")]
    public bool trainingMode;

    //O rigidbody do agent
    new private Rigidbody rigidbody;

    //Área de flores em que o agente está
    private FlowerArea flowerArea;

    //A flor mais próxima do agente
    private Flower nearestFlower;

    //Permite mudanças de tom mais suaves
    private float smoothPitchChange = 0f;

    //Permite mudanças de guinada mais suaves
    private float smoothYawChange = 0f;

    //ângulo máximo que o pássaro pode lançar para cima ou para baixo
    private const float MaxPitchAngle = 80f;

    //Distância máxima da ponta do bico para aceitar a colisão do néctar
    private const float BeakTipRadius = 0.008f;

    // Se o agente está congelado (intencionalmente não voando)
    private bool frozen = false;


    /// <summary>
    /// A quantidade de néctar que o agente obteve neste episódio
    /// </summary>
    public float NectarObtained { get; private set; }


    //Inicializa o agente
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        //Se não estiver no modo de traing, sem max step, jogue para sempre
        if (!trainingMode) MaxStep = 0;
    }


    /// <summary>
    /// Reinicialize o agente quando um episódio começar
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if(trainingMode)
        {
            //Somente redefina flores no treinamento quando houver um agente por área
            flowerArea.ResetFlowers();
        }

        //Redefini o néctar obtido
        NectarObtained = 0f;

        //Zera as velocidades para que o movimento pare antes que um novo episódio comece
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVeocity = Vector3.zero;

        //Default to spawning in front of a flower
        bool inFrontOfFlower = true;
        if(trainingMode)
        {
            //Spawn in front of flower 50% of the time during training
            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        //Move o agente para uma nova posição aleatória
        MoveToSafeRandomPosition(inFrontOfFlower);

        //Recalcula a flor mais próxima agora que o agente mudou
        UpdateNearestFlower();
    }


    /// <summary>
    /// Chamado quando uma ação é recebida da entrada do jogador ou da rede neural
    /// 
    /// vectorAction[i] representa:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        //Não tome providências se estiver congelado
        if (frozen) return;

        //calcula vetor de movimento
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        //Adicione força na direção do vetor de movimento
        rigidbody.AddForce(move * moveForce);

        //Obtenha a rotação atual
        Vector3 rotationVector = transform.rotation.eulerAngles;

        //Calcula a rotação de inclinação e guinada
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        //Calcula mudanças suaves de rotação
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);


        //Calcule a nova inclinação e guinada com base em valores suavizados
        //Afixa o passo para evitar virar de cabeça para baixo
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        //Aplicar a nova rotação
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }


    /// <summary>
    /// Colete observações vetoriais do ambiente
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //Se o mais próximo for nulo, observe uma matriz vazia e retorne mais cedo
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }
        
        //Observa a rotação local do agente (4 observações)
        sensor.AddObservation(transform.localRotation.normalized);

        //Obtem um vetor da ponta do bico até a flor mais próxima
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        //Observa um vetor normalizado apontando para a flor mais próxima (3 observações)
        sensor.AddObservation(toFlower.normalized);

        //Observa um produto escalar que indica se a ponta do bico está na frente da flor (1 observação)
        // (+1 significa que a ponta do bico está diretamente na frente da flor, -1 significa diretamente atrás)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        //Observa um produto escalar que indica se a ponta do bico está apontando para a flor (1 observação)
        // (+1 significa que a ponta do bico está apontando diretamente para a flor, -1 significa diretamente para longe)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        //Observa a distância relativa da ponta do bico à flor (1 observação)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        //10 observacoes
    }


    /// <summary>
    /// Quando o Tipo de comportamento é definido como "Somente heurística" nos Parâmetros de comportamento do agente,
    /// esta função será chamada. Seus valores de retorno serão alimentados em
    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">And output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        // Crie marcadores de posição para todos os movimentos / giros
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // Converte as entradas do teclado em movimento e giro
        //Todos os valores devem estar entre -1 e +1

        //Para frente / para trás
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        //Esquerda/direita
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        //Up/down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        //Pitch up/down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        //Vire esquerda/direita
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        //Combine o vetor de movimento e normalize
        Vector3 combined = (forward + left + up).normalized;

        //Adicione os 3 valores de movimento, pitch e yaw à matriz actionsOut
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;

    }


    /// <summary>
    /// Impede que o agente se mova e execute ações
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }


    /// <summary>
    /// Retoma o movimento e as ações do agente
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.Wakeup();
    }

    /// <summary>
    /// Move o agente para uma posição segura (ou seja, não colide com nada)
    /// Se estiver na frente da flor, aponte também o bico para a flor
    /// </summary>
    /// <param name="inFrontOfFlower">Se deve escolher um local em frente a uma flor</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; //Previne um loop infinito
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        //Faz um loop até que uma posição segura seja encontrada ou esgotadas as tentativas
        while(!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if(inFrontOfFlower)
            {
                //Escolhe uma flor aleatória
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                //Posiciona 10 a 20 cm na frente da flor
                float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                //Aponta o bico para a flor (a cabeça do pássaro é o centro da transformação)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialPosition = Quaternion.LookRotation(toFlower, Vector3.up);

            }

            else
            {
                //Escolhe uma altura aleatória do solo
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                //Escolhe um raio aleatório do centro da área
                float radius = UnityEngine.Random.Range(2f, 7f);

                //Escolhe uma direção aleatória girada em torno do eixo y
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                //Combina altura, raio e direção para escolher uma posição potencial
                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                //Escolhe e defina arremesso e guinada aleatórios
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            //Verifica se o agente irá colidir com alguma coisa
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            //A posição segura é encontrada se nenhum colisor estiver sobreposto
            safePositionFound = colliders.Length == 0;

        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        //Define a posição e rotação
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;


    }


    /// <summary>
    /// Atualiza a flor mais próxima do agente
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                //Nenhuma flor mais próxima atual e esta flor tem néctar, então define para esta flor
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                //Calcula a distância para esta flor e a distância para a flor mais próxima atual
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                //Se a flor mais próxima atual estiver vazia OU esta flor estiver mais perto, atualiza a flor mais próxima
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }


    /// <summary>
    /// Chamado quando o colisor do agente entra em um colisor de gatilho
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Chamado quando o colisor do agente permanece em um colisor de gatilho
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }


    /// <summary>
    /// Lida com quando o colisor do agente entra ou permanece em um colisor de gatilho
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        //Verifica se o agente está colidindo com o néctar
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            //Verifique se o ponto de colisão mais próximo está perto da ponta do bico
            //Nota: uma colisão com qualquer coisa, mas a ponta do bico não deve contar

            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                //procura a flor para este colisor de néctar
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                //Tenta tomar 0,01 néctar
                //Observação: isso ocorre por intervalo de tempo fixo, o que significa que acontece a cada 0,02 segundos ou 50x por segundo
                float nectarReceived = flower.Feed(.01f);

                //Acompanha o néctar obtido
                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    //Calcula a recompensa por obter néctar
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                //Se a flor estiver vazia, atualize a flor mais próxima
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Chamado quando o agente colide com algo sólido
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            //Colidiu com o limite da área, da uma recompensa negativa
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Chamada em cada frame
    /// </summary>
    private void Update()
    {
        //Desenha uma linha da ponta do bico até a flor mais próxima
        if (nearestFlower != null)
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
    }


    /// <summary>
    /// Chamado a cada 0,02 segundos
    /// </summary>
    private void FixedUpdate()
    {
        //Evita cenário em que o néctar da flor mais próximo é roubado pelo oponente e não é atualizado
        if (nearestFlower != null && !nearestFlower.HasNectar)
            UpdateNearestFlower();
    }

}
