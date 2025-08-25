import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subscription, timer } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { ThemeService } from '../core/services/theme.service';

declare const bootstrap: any; // Bootstrap JS runtime

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
  standalone: true,
  imports: [CommonModule, RouterModule],
})
export class NavbarComponent implements OnInit, AfterViewInit, OnDestroy {
  currentUser: any;

  // --- Modal reference ---
  @ViewChild('sessionExpiredModal', { static: true }) modalEl!: ElementRef;
  private modal?: any;

  // --- Timers & subs ---
  private sessionTimer: any;
  private authSub?: Subscription;

  constructor(
    public authService: AuthService,
    private router: Router,
    public theme: ThemeService
  ) {}

  // THEME
  toggleTheme() {
    this.theme.toggle();
  }
  get isDark() {
    return this.theme.theme === 'dark';
  }
  get logoPath(): string {
    return this.theme.theme === 'dark'
      ? 'assets/logo white.png'
      : 'assets/logo black.png';
  }

  // LIFECYCLE
  ngOnInit(): void {
    this.currentUser = JSON.parse(localStorage.getItem('currentUser') as string);
    this.setSessionTimer();

    // Reset timer whenever token/user changes (e.g., after extendToken)
    this.authSub = this.authService.currentUser.subscribe(() => {
      this.currentUser = JSON.parse(localStorage.getItem('currentUser') as string);
      this.resetSessionTimer();
    });
  }

  ngAfterViewInit(): void {
    // Create Bootstrap modal instance
    this.modal = new bootstrap.Modal(this.modalEl.nativeElement, {
      backdrop: 'static',
      keyboard: false,
    });
  }

  ngOnDestroy(): void {
    this.clearSessionTimer();
    this.authSub?.unsubscribe();
  }

  // NAV
  goToInventory() {
    this.router.navigate(['/inventory']);
  }

  // LOGOUT (manual button)
  logout(): void {
    if (confirm('Vil du gerne logge ud?')) {
      this.authService.logout();
      this.router.navigate(['/login']);
    }
  }

  // ===== Session modal actions =====
  onConfirmExtend(): void {
    // User clicked "Yes"
    this.authService
      .extendToken()
      .pipe(
        finalize(() => {
          // Hide modal either way; if request fails we logout below
          this.hideModal();
        })
      )
      .subscribe({
        next: () => {
          // Successfully extended -> reschedule timer
          this.resetSessionTimer();
        },
        error: () => {
          // Extend failed -> log out
          this.authService.logout();
          this.router.navigate(['/login']);
        },
      });
  }

  onDeclineExtend(): void {
    // User clicked "No"
    this.hideModal();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private showModal(): void {
    if (this.modal) this.modal.show();
  }
  private hideModal(): void {
    if (this.modal) this.modal.hide();
  }

  // ===== Session timer logic (stays in Navbar) =====
  private setSessionTimer(): void {
    const remaining = this.authService.getRemainingTokenTime();

    // Show modal when token expires; if already near expiry, give a small grace window (10s)
    const delay = remaining > 10000 ? remaining : 10000;

    this.sessionTimer = setTimeout(() => {
      this.showModal();
    }, delay);
  }

  private clearSessionTimer(): void {
    if (this.sessionTimer) {
      clearTimeout(this.sessionTimer);
      this.sessionTimer = null;
    }
  }

  private resetSessionTimer(): void {
    this.clearSessionTimer();
    this.setSessionTimer();
  }
}
