import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { EMPTY, catchError } from 'rxjs';
import { AuthService } from './core/auth.service';
import { CsrfService } from './core/csrf.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly csrf = inject(CsrfService);
  private readonly router = inject(Router);

  protected readonly currentUser = this.auth.currentUser;
  protected readonly isUser = computed(() => this.currentUser()?.role === 'User');
  protected readonly isAdmin = computed(() => this.currentUser()?.role === 'SuperAdmin');

  protected roleLabel(role: string): string {
    return role === 'SuperAdmin' ? 'Сессия суперадминистратора' : 'Личный кабинет';
  }

  protected sessionIdentity(): string | null {
    const user = this.currentUser();
    if (!user) {
      return null;
    }

    return user.email || user.login;
  }

  ngOnInit(): void {
    this.csrf.ensureToken().pipe(catchError(() => EMPTY)).subscribe();
    this.auth.refreshSession().pipe(catchError(() => EMPTY)).subscribe();
  }

  protected logout(): void {
    this.auth.logout().pipe(catchError(() => EMPTY)).subscribe(() => {
      void this.router.navigateByUrl('/');
    });
  }
}
