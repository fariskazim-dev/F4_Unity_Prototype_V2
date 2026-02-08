using UnityEngine;

public class RainCloud : MonoBehaviour
{
   //GOAL:
   //The rain drop should fall onto the fire, then be boosted up on the Y axis to a new height. 
   //The rain drop should remain at this Y height until a timer runs out. 
   //When the timer runs out, the rain drop should fall down. 
   
   private Rigidbody rb; //How we will control the raindrop 
   [SerializeField] private float heightToAdd = 5; //The amount of height we will add to Y 
   private float cloudHeight; //total Y height the cloud will be held at 

   [SerializeField] private float cloudTimerMax = 15;
   [SerializeField] private float cloudTimer;
   [SerializeField] private float cloudSpeed = 2; 

   void Awake()
   {
      rb = GetComponent<Rigidbody>();
   }

   void OnTriggerEnter(Collider other)
   {
      if (other.gameObject.tag == "Fire")
      {
         print("Collided with fire");
         cloudTimer = cloudTimerMax;
         Vector3 newPosition = new Vector3(rb.position.x, rb.position.y + heightToAdd, rb.position.z); 
         rb.MovePosition(newPosition);
      }
   }

   void FixedUpdate()
   {
      if (cloudTimer > 0)
      {
         print("cloud timer >=0 called");
         rb.useGravity = false; 
         cloudTimer -= Time.fixedDeltaTime;
      }
      else if (cloudTimer <= 0)
      {
         print("cloud timer else called"); 
         rb.useGravity = true; 
      }
   }





}
