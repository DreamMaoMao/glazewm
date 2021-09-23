﻿using System.Linq;
using GlazeWM.Domain.Containers;
using GlazeWM.Domain.Containers.Commands;
using GlazeWM.Domain.Monitors;
using GlazeWM.Domain.Windows;
using GlazeWM.Domain.Workspaces.Commands;
using GlazeWM.Infrastructure.Bussing;

namespace GlazeWM.Domain.Workspaces.CommandHandlers
{
  class DisplayWorkspaceHandler : ICommandHandler<DisplayWorkspaceCommand>
  {
    private Bus _bus;
    private MonitorService _monitorService;
    private ContainerService _containerService;

    public DisplayWorkspaceHandler(Bus bus, MonitorService monitorService, ContainerService containerService)
    {
      _bus = bus;
      _monitorService = monitorService;
      _containerService = containerService;
    }

    public CommandResponse Handle(DisplayWorkspaceCommand command)
    {
      var workspaceToDisplay = command.Workspace;

      var monitor = _monitorService.GetMonitorFromChildContainer(command.Workspace);
      var currentWorkspace = monitor.DisplayedWorkspace;

      // If DisplayedWorkspace is unassigned, there is no need to show/hide windows.
      if (currentWorkspace == null)
      {
        monitor.DisplayedWorkspace = workspaceToDisplay;
        return CommandResponse.Ok;
      }

      if (currentWorkspace == workspaceToDisplay)
        return CommandResponse.Ok;

      var windowsToHide = currentWorkspace.Children
          .SelectMany(container => container.Flatten())
          .OfType<Window>()
          .ToList();

      foreach (var window in windowsToHide)
        window.IsHidden = true;

      var windowsToShow = workspaceToDisplay.Children
          .SelectMany(container => container.Flatten())
          .OfType<Window>()
          .ToList();

      foreach (var window in windowsToShow)
        window.IsHidden = false;

      monitor.DisplayedWorkspace = command.Workspace;

      _containerService.SplitContainersToRedraw.Add(currentWorkspace);
      _containerService.SplitContainersToRedraw.Add(workspaceToDisplay);

      _bus.Invoke(new RedrawContainersCommand());

      return CommandResponse.Ok;
    }
  }
}