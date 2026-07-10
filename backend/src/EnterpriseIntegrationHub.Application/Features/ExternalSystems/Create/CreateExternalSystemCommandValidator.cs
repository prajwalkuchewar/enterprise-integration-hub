using System;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;

public sealed class CreateExternalSystemCommandValidator
{
  public void Validate(CreateExternalSystemCommand command)
  {
    if (command is null)
      throw new ArgumentNullException(nameof(command));

    if (string.IsNullOrWhiteSpace(command.Name))
      throw new ArgumentException("Name is required.", nameof(command.Name));

  }
}
