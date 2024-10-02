using UnityEngine;

public class GravityInverter : MonoBehaviour
{
	private Collider floorCollider;
	private bool canInvertGravity;

	public void Start()
	{
		var colliders = GetComponentsInParent<Collider>();
		foreach (var collider in colliders)
		{
			if (collider.gameObject != this.gameObject)
			{
				floorCollider = collider;
				break;
			}
		}
	}

	private bool TryGetPlayerIfCanInvert(Collider other, out PlayerMovement player) {
		player = null;
		if (other.gameObject.CompareTag("Player")) {
			var p = other.gameObject.GetComponent<PlayerMovement>();
			if (p != null && p.canInvertGravity())
			{
				player = p;
			}
		}
		return player != null;
	}

	private void TryReadyPlayerInversion(Collider other)
	{
		if (!canInvertGravity && TryGetPlayerIfCanInvert(other, out PlayerMovement player))
		{
			floorCollider.enabled = false;
			canInvertGravity = true;
		}
	}

	private void InvertGravity(PlayerMovement player)
	{
		floorCollider.enabled = true;
		player.UpdateGravityDirection(Vector3.Scale(-player.velocity, this.transform.up).normalized);
		canInvertGravity = false;
	}

	public void OnTriggerStay(Collider other)
	{
		bool playerCanInvert = TryGetPlayerIfCanInvert(other, out PlayerMovement player);
		if (!canInvertGravity)
		{
			if (playerCanInvert)
			{
				floorCollider.enabled = false;
				canInvertGravity = true;
			}
		}
		else
		{
			var d = Vector3.Scale(other.transform.position - this.transform.position, this.transform.up);
			var newGravity = Vector3.Scale(-player.velocity, this.transform.up).normalized;
			var ph = player.Height;
			if(Vector3.Dot(d, newGravity) < 0 && d.sqrMagnitude >= ph*ph) {
				InvertGravity(player);
			}
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		TryReadyPlayerInversion(other);
	}

	public void OnTriggerExit(Collider other)
	{
		if (TryGetPlayerIfCanInvert(other, out PlayerMovement player)) {
			InvertGravity(player);
		}
	}
}
