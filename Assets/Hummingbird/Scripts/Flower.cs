using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desenvolve uma unica flor com nectar
/// </summary>

public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full")]
    public Color fullFlowerColor = new Color(1f, 0f, .3f);

    [Tooltip("The color when the flower is empty")]
    public Color emptyFlowerColor = new Color(.5f, 0f, 1f);


    /// <summary>
    /// O colisor tigger representando o nectar
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;

    //O colisor solido que representa as petalas da flor
    private Collider flowerCollider;

    //O material da flor
    private Material flowerMaterial;

    /// <summary>
    /// Um vetor apontando para fora da flor
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }


    /// <summary>
    /// A posição central do colisor de nectar
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }


    /// <summary>
    /// A quantidade de nectar restante na flor
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    /// Se a flor tem algum nectar restante
    /// </summary>
    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }


    /// <summary>
    /// Tenta remover o nectar da flor
    /// </summary>
    /// <param name="amount"> A quantidade de nectar a ser removida</param>
    /// <returns>O valor real removido com sucesso</returns>
    public float Feed(float amount)
    {
        // Rastreia quanto nectar foi retirado com sucesso (não pode demorar mais do que o disponível)
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        //Subtraia o nectar
        NectarAmount -= amount;

        if (NectarAmount <= 0)
        {
            //Sem nectar germinando
            NectarAmount = 0;

            //Desativa os coletores de flor e néctar
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            //Muda a cor da flor para indicar que está vazia
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);

        }

        // Retorna a quantidade de nectar que foi retirada
        return nectarTaken;

    }


    /// <summary>
    /// Redefine a flor
    /// </summary>
    public void ResetFlower()
    {
        //Refill o nectar
        NectarAmount = 1f;

        //Habilita os coletores de flores e néctares
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        //Muda a cor da flor para indicar que está cheia
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    /// <summary>
    /// Chamado quando a flor acorda
    /// </summary>
    private void Awake()
    {
        //Encontra o renderizador de malha de flores e obtem o material principal
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        //Encontra coletores de flores e nectares
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }

}
