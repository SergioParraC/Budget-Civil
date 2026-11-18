import { AfterViewInit, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

import { AuthService } from '../../../core/auth/auth.service';
import { resolveApiError } from '../../../core/http/api-error';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    MessageModule,
    IconFieldModule,
    InputIconModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login implements AfterViewInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  @ViewChild('identifierInput') private readonly identifierInput?: ElementRef<HTMLInputElement>;
  @ViewChild('passwordInput') private readonly passwordInputHost?: ElementRef<HTMLElement>;

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    identifier: ['', Validators.required],
    password: ['', Validators.required]
  });

  ngAfterViewInit(): void {
    this.identifierInput?.nativeElement.focus();
  }

  get identifierInvalid(): boolean {
    const control = this.form.controls.identifier;
    return control.invalid && (control.touched || control.dirty);
  }

  get passwordInvalid(): boolean {
    const control = this.form.controls.password;
    return control.invalid && (control.touched || control.dirty);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.focusFirstInvalidField();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.form.disable({ emitEvent: false });

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.form.enable({ emitEvent: false });
        this.errorMessage.set(resolveApiError(error).message);
        this.identifierInput?.nativeElement.focus();
      }
    });
  }

  private focusFirstInvalidField(): void {
    if (this.identifierInvalid) {
      this.identifierInput?.nativeElement.focus();
      return;
    }

    if (this.passwordInvalid) {
      this.focusPasswordInput();
    }
  }

  private focusPasswordInput(): void {
    this.passwordInputHost?.nativeElement.querySelector('input')?.focus();
  }
}
