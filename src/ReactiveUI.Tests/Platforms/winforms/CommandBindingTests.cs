﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using ReactiveUI.Winforms;
using Xunit;

namespace ReactiveUI.Tests.Winforms
{
    public class CommandBindingTests
    {
        [Fact]
        public void CommandBinderBindsToButton()
        {
            var fixture = new CreatesWinformsCommandBinding();
            var cmd = ReactiveCommand.Create<int>(_ => { });
            var input = new Button { };

            Assert.True(fixture.GetAffinityForObject(input.GetType(), true) > 0);
            Assert.True(fixture.GetAffinityForObject(input.GetType(), false) > 0);
            var commandExecuted = false;
            object ea = null;
            cmd.Subscribe(o =>
            {
                ea = o;
                commandExecuted = true;
            });

            using (fixture.BindCommandToObject(cmd, input, Observable.Return((object)5)))
            {
                input.PerformClick();

                Assert.True(commandExecuted);
                Assert.NotNull(ea);
            }
        }

        [Fact]
        public void CommandBinderBindsToCustomControl()
        {
            var fixture = new CreatesWinformsCommandBinding();
            var cmd = ReactiveCommand.Create<int>(_ => { });
            var input = new CustomClickableControl { };

            Assert.True(fixture.GetAffinityForObject(input.GetType(), true) > 0);
            Assert.True(fixture.GetAffinityForObject(input.GetType(), false) > 0);
            var commandExecuted = false;
            object ea = null;
            cmd.Subscribe(o =>
            {
                ea = o;
                commandExecuted = true;
            });

            using (fixture.BindCommandToObject(cmd, input, Observable.Return((object)5)))
            {
                input.PerformClick();

                Assert.True(commandExecuted);
                Assert.NotNull(ea);
            }
        }

        [Fact]
        public void CommandBinderBindsToCustomComponent()
        {
            var fixture = new CreatesWinformsCommandBinding();
            var cmd = ReactiveCommand.Create<int>(_ => { });
            var input = new CustomClickableComponent { };

            Assert.True(fixture.GetAffinityForObject(input.GetType(), true) > 0);
            Assert.True(fixture.GetAffinityForObject(input.GetType(), false) > 0);
            var commandExecuted = false;
            object ea = null;
            cmd.Subscribe(o =>
            {
                ea = o;
                commandExecuted = true;
            });

            using (fixture.BindCommandToObject(cmd, input, Observable.Return((object)5)))
            {
                input.PerformClick();

                Assert.True(commandExecuted);
                Assert.NotNull(ea);
            }
        }

        [Fact]
        public void CommandBinderAffectsEnabledState()
        {
            var fixture = new CreatesWinformsCommandBinding();
            var canExecute = new Subject<bool>();
            canExecute.OnNext(true);

            var cmd = ReactiveCommand.Create(() => { }, canExecute);
            var input = new Button { };

            using (fixture.BindCommandToObject(cmd, input, Observable.Return((object)5)))
            {
                canExecute.OnNext(true);
                Assert.True(input.Enabled);

                canExecute.OnNext(false);
                Assert.False(input.Enabled);
            }
        }

        [Fact]
        public void CommandBinderAffectsEnabledStateForComponents()
        {
            var fixture = new CreatesWinformsCommandBinding();
            var canExecute = new Subject<bool>();
            canExecute.OnNext(true);

            var cmd = ReactiveCommand.Create(() => { }, canExecute);
            var input = new ToolStripButton { }; // ToolStripButton is a Component, not a Control

            using (fixture.BindCommandToObject(cmd, input, Observable.Return((object)5)))
            {
                canExecute.OnNext(true);
                Assert.True(input.Enabled);

                canExecute.OnNext(false);
                Assert.False(input.Enabled);
            }
        }
    }

    public class CustomClickableComponent : Component
    {
        public event EventHandler Click;

        public void PerformClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CustomClickableControl : Control
    {
        public void PerformClick()
        {
            InvokeOnClick(this, EventArgs.Empty);
        }

        public void RaiseMouseClickEvent(MouseEventArgs args)
        {
            OnMouseClick(args);
        }

        public void RaiseMouseUpEvent(MouseEventArgs args)
        {
            OnMouseUp(args);
        }
    }

    public class CommandBindingImplementationTests
    {
        [Fact]
        public void CommandBindByNameWireup()
        {
            var vm = new WinformCommandBindViewModel();
            var view = new WinformCommandBindView { ViewModel = vm };
            var fixture = new CommandBinderImplementation();

            var invokeCount = 0;
            vm.Command1.Subscribe(_ => invokeCount += 1);

            var disp = fixture.BindCommand(vm, view, x => x.Command1, x => x.Command1);

            view.Command1.PerformClick();

            Assert.Equal(1, invokeCount);

            var newCmd = ReactiveCommand.Create(() => { });
            vm.Command1 = newCmd;

            view.Command1.PerformClick();
            Assert.Equal(1, invokeCount);

            disp.Dispose();
        }

        [Fact]
        public void CommandBindToExplicitEventWireup()
        {
            var vm = new WinformCommandBindViewModel();
            var view = new WinformCommandBindView { ViewModel = vm };
            var fixture = new CommandBinderImplementation();

            var invokeCount = 0;
            vm.Command2.Subscribe(_ => invokeCount += 1);

            var disp = fixture.BindCommand(vm, view, x => x.Command2, x => x.Command2, "MouseUp");

            view.Command2.RaiseMouseUpEvent(new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            disp.Dispose();

            view.Command2.RaiseMouseUpEvent(new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
            Assert.Equal(1, invokeCount);
        }
    }

    public class FakeViewModel : ReactiveObject
    {
        public FakeViewModel()
        {
            Cmd = ReactiveCommand.Create(() => { });
        }

        public ReactiveCommand<Unit, Unit> Cmd { get; protected set; }
    }

    public class FakeView : IViewFor<FakeViewModel>
    {
        public FakeView()
        {
            TheTextBox = new TextBox();
            ViewModel = new FakeViewModel();
        }

        public TextBox TheTextBox { get; protected set; }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (FakeViewModel)value;
        }

        public FakeViewModel ViewModel { get; set; }
    }

    public class WinformCommandBindViewModel : ReactiveObject
    {
        private ReactiveCommand<Unit, Unit> _command1;

        private ReactiveCommand<Unit, Unit> _command2;

        public WinformCommandBindViewModel()
        {
            Command1 = ReactiveCommand.Create(() => { });
            Command2 = ReactiveCommand.Create(() => { });
        }

        public ReactiveCommand<Unit, Unit> Command1
        {
            get => _command1;
            set => this.RaiseAndSetIfChanged(ref _command1, value);
        }

        public ReactiveCommand<Unit, Unit> Command2
        {
            get => _command2;
            set => this.RaiseAndSetIfChanged(ref _command2, value);
        }
    }

    public class WinformCommandBindView : IViewFor<WinformCommandBindViewModel>
    {
        public WinformCommandBindView()
        {
            Command1 = new Button();
            Command2 = new CustomClickableControl();
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (WinformCommandBindViewModel)value;
        }

        public WinformCommandBindViewModel ViewModel { get; set; }

        public Button Command1 { get; protected set; }

        public CustomClickableControl Command2 { get; protected set; }
    }
}
