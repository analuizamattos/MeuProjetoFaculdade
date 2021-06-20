using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia uma coleção de plantas floridas e flores anexas
/// </summary>
public class FlowerArea : MonoBehaviour
{
    // O diametro da área onde o agente e as flores podem estar
    //usado para observar a distancia relativa do agente a flor
    public const float AreaDiameter = 20f;

    //A lista de todas as plantas com flores nesta área de flores (as plantas com flores têm várias flores)
    private List<GameObject> flowerPlants;

    //Um dicionário de pesquisa para procurar uma flor em um colisor de néctar
    private Dictionary<Collider, Flower> nectarFlowerDictionary;


    /// <summary>
    /// A lista de todas as flores na área das flores
    /// </summary>
    public List<Flower> Flowers { get; private set; }

    /// <summary>
    /// Reinicia as flores e plantas de flores
    /// </summary>
    public void ResetFlowers()
    {
        //Gira cada planta de flor em torno do eixo Y e aproximadamente em torno de x e z
        foreach(GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);

            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        //Reinicia cada flor
        foreach(Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }


    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar collider belongs to
    /// </summary>
    /// <param name="collider">The nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }


    /// <summary>
    /// Chamado quando a área acorda
    /// </summary>
    private void Awake()
    {
        //inicializa variáveis
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }


    /// <summary>
    /// Chamado quando o jogo começa
    /// </summary>
    private void Start()
    {
        //Encontra todas as flores que são filhas deste GameObject / Transform
        FindChildFlowers(transform);
    }


    /// <summary>
    /// Encontra flores recursivamente e plantas de flores que são filhas de um pai transformado
    /// </summary>
    /// <param name="parent">O pai dos filhos para verificar</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i <parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            
            if(child.CompareTag("flower_plant"))
            {
                //Encontrou uma planta de flor, adicione-a à lista de plantas de flor
                flowerPlants.Add(child.gameObject);

                //Procure flores dentro da flor da planta
                FindChildFlowers(child);
            }
            else
            {
                //Não é uma planta de flor, procura um componente de flor
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    //Encontrou uma flor, adicione-a à lista de flores
                    Flowers.Add(flower);

                    // Adicione o colisor de néctar ao dicionário de pesquisa
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);

                    //Nota: Não há flores que sejam filhas de outras flores

                }
                else
                {
                    // O componente da flor não foi encontrado, então verifique os filhos
                    FindChildFlowers(child);
                }

            }
        }
    }

}
