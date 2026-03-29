import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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

  protected readonly currentUser = this.auth.currentUser;

  ngOnInit(): void {
    this.csrf.ensureToken().pipe(catchError(() => EMPTY)).subscribe();
    this.auth.refreshSession().pipe(catchError(() => EMPTY)).subscribe();
  }

  protected logout(): void {
    this.auth.logout().pipe(catchError(() => EMPTY)).subscribe();
  }
}
