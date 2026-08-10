using System;

public sealed class DieRollSettleTracker
{
	private readonly float _linearSpeedThreshold;
	private readonly float _angularSpeedThreshold;
	private readonly float _confirmationSeconds;
	private float _stableSeconds;

	public DieRollSettleTracker(
		float linearSpeedThreshold,
		float angularSpeedThreshold,
		float confirmationSeconds)
	{
		if (linearSpeedThreshold < 0f)
			throw new ArgumentOutOfRangeException(nameof(linearSpeedThreshold));
		if (angularSpeedThreshold < 0f)
			throw new ArgumentOutOfRangeException(nameof(angularSpeedThreshold));
		if (confirmationSeconds < 0f)
			throw new ArgumentOutOfRangeException(nameof(confirmationSeconds));

		_linearSpeedThreshold = linearSpeedThreshold;
		_angularSpeedThreshold = angularSpeedThreshold;
		_confirmationSeconds = confirmationSeconds;
	}

	public bool Observe(
		float linearSpeed,
		float angularSpeed,
		float elapsedSeconds,
		bool hasSupportContact = true)
	{
		if (elapsedSeconds < 0f)
			throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));

		if (hasSupportContact &&
			linearSpeed <= _linearSpeedThreshold &&
			angularSpeed <= _angularSpeedThreshold)
			_stableSeconds += elapsedSeconds;
		else
			_stableSeconds = 0f;

		return _stableSeconds >= _confirmationSeconds;
	}
}
