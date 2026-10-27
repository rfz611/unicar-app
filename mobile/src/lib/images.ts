import * as ImagePicker from 'expo-image-picker';
import { Platform } from 'react-native';
import { ApiError } from '@/api/client';

export type PickedImage = ImagePicker.ImagePickerAsset;

/**
 * Escolhe uma foto da galeria (ou tira com a câmera). null se a pessoa desistir.
 * No navegador a câmera vira o seletor de arquivos.
 */
export async function pickImage({ camera = false, square = false }: { camera?: boolean; square?: boolean } = {}): Promise<PickedImage | null> {
  const options: ImagePicker.ImagePickerOptions = {
    mediaTypes: ['images'],
    allowsEditing: square,
    aspect: square ? [1, 1] : undefined,
    quality: 0.8,
    cameraType: ImagePicker.CameraType.front,
  };
  if (camera && Platform.OS !== 'web') {
    const perm = await ImagePicker.requestCameraPermissionsAsync();
    if (!perm.granted) throw new ApiError('camera_denied', 'Libere a câmera nos ajustes do celular para tirar a foto.', 0);
    const res = await ImagePicker.launchCameraAsync(options);
    return res.canceled ? null : (res.assets[0] ?? null);
  }
  const res = await ImagePicker.launchImageLibraryAsync(options);
  return res.canceled ? null : (res.assets[0] ?? null);
}

/** Monta o multipart "file" que a API espera (jpg, png ou webp até 5 MB). */
export async function imageForm(image: PickedImage) {
  const form = new FormData();
  const name = image.fileName ?? 'foto.jpg';
  if (Platform.OS === 'web') {
    const file = image.file ?? (await (await fetch(image.uri)).blob());
    form.append('file', file, name);
  } else {
    // React Native envia o arquivo pelo uri
    form.append('file', { uri: image.uri, name, type: image.mimeType ?? 'image/jpeg' } as unknown as Blob);
  }
  return form;
}
