using UnityEngine;
using System.Collections;
using Mirror;
[RequireComponent(typeof(AudioSource))]
public class Sound : NetworkBehaviour
{
	AudioSource source;
	public AudioClip[] clips;
	// Use this for initialization
	void Start()
	{
		source = this.GetComponent<AudioSource>();
	}

	public void playSound(int id)
	{
		if (!isLocalPlayer)
			return;
		if (id >= 0 && id < clips.Length)
		{
			CmdSendServerSoundID(id);
		}
	}
	[Command]
	void CmdSendServerSoundID(int id)
	{
		RpcSendSoundIdToClients(id);
	}
	[ClientRpc]
	void RpcSendSoundIdToClients(int id)
	{
		source.PlayOneShot(clips[id]);
	}

}
