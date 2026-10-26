import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

/**
 * Guarda o token JWT. No celular: SecureStore (Keychain/Keystore).
 * No navegador (só para desenvolvimento) o SecureStore não existe, então usa localStorage.
 */
const KEY = 'unicar.token';

export async function loadToken(): Promise<string | null> {
  if (Platform.OS === 'web') {
    try {
      return globalThis.localStorage?.getItem(KEY) ?? null;
    } catch {
      return null;
    }
  }
  return SecureStore.getItemAsync(KEY);
}

export async function saveToken(token: string | null): Promise<void> {
  if (Platform.OS === 'web') {
    try {
      if (token) globalThis.localStorage?.setItem(KEY, token);
      else globalThis.localStorage?.removeItem(KEY);
    } catch {
      /* navegador sem storage: segue só em memória */
    }
    return;
  }
  if (token) await SecureStore.setItemAsync(KEY, token);
  else await SecureStore.deleteItemAsync(KEY);
}
