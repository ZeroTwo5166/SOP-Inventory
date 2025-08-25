// auth.service.ts
import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../environments/environment';
import { Login } from '../models/login';
import { jwtDecode } from 'jwt-decode';

interface JwtPayload { exp: number; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject: BehaviorSubject<Login | null>;
  public currentUser: Observable<Login | null>;
  private pendingLoginUser: Login | null = null;

  private expiryTimer: any = null;
  private popupShown = false;

  // 🔔 NEW: UI listens to this to show the modal
  public readonly sessionExpired$ = new BehaviorSubject<boolean>(false);

  constructor(
    private http: HttpClient,
    private router: Router,
    private ngZone: NgZone
  ) {
    let user: Login | null = null;
    if (typeof localStorage !== 'undefined') {
      user = JSON.parse(localStorage.getItem('currentUser') as string);
    }
    this.currentUserSubject = new BehaviorSubject<Login | null>(user);
    this.currentUser = this.currentUserSubject.asObservable();

    if (user && user.token && this.getTokenExpiration(user.token) <= Date.now()) {
      this.logout();
    } else if (user?.token) {
      this.scheduleExpiryPopup(user.token);
    }
  }

  public get currentUserValue(): Login | null {
    return this.currentUserSubject.value;
  }

  login(email: string, password: string, role: string) {
    const authenticateUrl = `${environment.apiUrl}User/authenticate`;
    return this.http.post<Login>(authenticateUrl, { email, password, role }).pipe(
      map((user) => { this.pendingLoginUser = user; return user; })
    );
  }

  logout() {
    if (this.expiryTimer) { clearTimeout(this.expiryTimer); this.expiryTimer = null; }
    this.popupShown = false;
    this.sessionExpired$.next(false); // ensure modal closes
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getQrCode(email:string): Observable<{ qrCodeImage: string }> {
    return this.http.get<{ qrCodeImage: string }>(
      `${environment.apiUrl}User/2fa?email=${encodeURIComponent(email)}`
    );
  }

  verifyOtp(email: string, code: string): Observable<any> {
    return this.http.post<Login>(`${environment.apiUrl}User/2fa/verify`, { email, code }).pipe(
      map((res) => {
        if (this.pendingLoginUser) {
          localStorage.setItem('currentUser', JSON.stringify(this.pendingLoginUser));
          this.currentUserSubject.next(this.pendingLoginUser);
          if (this.pendingLoginUser.token) this.scheduleExpiryPopup(this.pendingLoginUser.token);
          this.pendingLoginUser = null;
        } else {
          console.warn('[verifyOtp] No pending login user found.');
        }
        return res;
      })
    );
  }

  private getTokenExpiration(token: string): number {
    try { return jwtDecode<JwtPayload>(token).exp * 1000; } catch { return 0; }
  }

  public getRemainingTokenTime(): number {
    const currentUser = this.currentUserValue;
    if (currentUser?.token) {
      const remaining = this.getTokenExpiration(currentUser.token) - Date.now();
      return remaining > 0 ? remaining : 0;
    }
    return 0;
  }

  public extendToken(): Observable<any> {
    const currentUser = this.currentUserValue;
    return this.http.post<any>(`${environment.apiUrl}User/extend-token`, { token: currentUser?.token }).pipe(
      map((response: any) => {
        const newToken = response.Token || response.token;
        if (currentUser && newToken) {
          const updatedUser = { ...currentUser, token: newToken };
          localStorage.setItem('currentUser', JSON.stringify(updatedUser));
          this.currentUserSubject.next(updatedUser);
          this.scheduleExpiryPopup(newToken);
        }
        return response;
      })
    );
  }

  // ===== Expiry popup scheduling =====
  private scheduleExpiryPopup(token: string) {
    if (this.expiryTimer) { clearTimeout(this.expiryTimer); this.expiryTimer = null; }
    const ms = this.getTokenExpiration(token) - Date.now();
    if (ms <= 0) { this.triggerExpiryPopup(); return; }
    this.expiryTimer = setTimeout(() => this.triggerExpiryPopup(), ms);
  }

  private triggerExpiryPopup() {
    if (this.popupShown) return;
    this.popupShown = true;
    // Let UI show modal (Yes/No)
    this.ngZone.run(() => this.sessionExpired$.next(true));
  }

  // Called by UI after user picks Yes/No
  public closeExpiryPopup() {
    this.popupShown = false;
    this.sessionExpired$.next(false);
  }

  // For 401s
  public onUnauthorized() {
    if (this.popupShown) return;
    this.triggerExpiryPopup();
  }
}
