import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useRef, useState, type ComponentRef } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { api, messageOf } from '@/api/client';
import type { ChatMessage } from '@/api/types';
import { Icon } from '@/components/Icon';
import { Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Chip, Field, IconButton, initialsOf } from '@/components/ui';
import { useHubEvent, useRealtime } from '@/realtime/RealtimeProvider';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

/** Conversa da corrida ou da carona marcada (id = conversationId). Só participantes: a API responde 403 aos outros. */
export default function Chat() {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const { user } = useSession();
  const { invoke } = useRealtime();
  const { sayError, say } = useToast();
  const { id, title = 'Conversa', context = '' } = useLocalSearchParams<{ id: string; title?: string; context?: string }>();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [quick, setQuick] = useState<string[]>([]);
  const [draft, setDraft] = useState('');
  const [typingUntil, setTypingUntil] = useState(0);
  const [now, setNow] = useState(() => Date.now());
  const lastTyping = useRef(0);
  const scroll = useRef<ComponentRef<typeof ScrollView>>(null);

  useEffect(() => {
    api.get<ChatMessage[]>(`/api/conversations/${id}/messages`).then(setMessages, (e) => {
      say(messageOf(e));
      router.back();
    });
    api.get<string[]>('/api/conversations/quick-replies').then(setQuick, () => {});
  }, [id, say]);

  const add = (m: ChatMessage) => setMessages((old) => (old.some((x) => x.id === m.id) ? old : [...old, m]));

  useHubEvent<ChatMessage>('MessageReceived', (m) => {
    if (m.conversationId !== id) return;
    add(m);
    if (m.authorId !== user?.id) setTypingUntil(0);
  });
  useHubEvent<{ conversationId: string; userId: string }>('Typing', (e) => {
    if (e.conversationId === id && e.userId !== user?.id) {
      const until = Date.now() + 3000;
      setTypingUntil(until);
      setNow(Date.now());
      setTimeout(() => setNow(Date.now()), 3100);
    }
  });

  const send = async (text: string) => {
    const clean = text.trim();
    if (!clean) return;
    setDraft('');
    try {
      add(await invoke<ChatMessage>('SendMessage', id, clean));
    } catch (e) {
      // sem tempo real (rede instável): manda pela API normal
      try {
        add(await api.post<ChatMessage>(`/api/conversations/${id}/messages`, { text: clean }));
      } catch (e2) {
        sayError(e2 ?? e);
        setDraft(clean);
      }
    }
  };

  const onDraft = (v: string) => {
    setDraft(v);
    const tNow = Date.now();
    if (v && tNow - lastTyping.current > 2000) {
      lastTyping.current = tNow;
      invoke('Typing', id).catch(() => {});
    }
  };

  const typing = typingUntil > now;

  return (
    <KeyboardAvoidingView style={{ flex: 1, backgroundColor: t.bg }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, paddingTop: Math.max(insets.top, 12) + 8, paddingHorizontal: 16, paddingBottom: 12, borderBottomWidth: 1, borderBottomColor: t.line }}>
        <IconButton icon="back" label="Voltar" onPress={() => router.back()} />
        <View style={{ width: 40, height: 40, borderRadius: 20, backgroundColor: t.s3, alignItems: 'center', justifyContent: 'center' }}>
          <Text size={14} weight={600}>
            {initialsOf(title)}
          </Text>
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Text size={18} weight={700} tracking={-0.02}>
            {title}
          </Text>
          <Text size={13} color={t.mut2} numberOfLines={1}>
            Universitário verificado
          </Text>
        </View>
      </View>

      <ScrollView
        ref={scroll}
        style={{ flex: 1 }}
        contentContainerStyle={{ flexGrow: 1, justifyContent: 'flex-end', padding: 16, gap: 8 }}
        onContentSizeChange={() => scroll.current?.scrollToEnd({ animated: true })}
        keyboardShouldPersistTaps="handled"
      >
        {context ? <Pill text={context} /> : null}
        {messages.map((m) =>
          m.kind === 'System' ? (
            <Pill key={m.id} text={m.text} />
          ) : (
            <Bubble key={m.id} mine={m.authorId === user?.id} text={m.text} />
          ),
        )}
        {typing ? (
          <Text size={13} color={t.mut2} style={{ alignSelf: 'flex-start', paddingVertical: 4, paddingHorizontal: 6 }}>
            digitando…
          </Text>
        ) : null}
      </ScrollView>

      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ flexGrow: 0 }} contentContainerStyle={{ gap: 8, paddingHorizontal: 16, paddingBottom: 10 }} keyboardShouldPersistTaps="handled">
        {quick.map((q) => (
          <Chip key={q} label={q} onPress={() => send(q)} />
        ))}
      </ScrollView>

      <View style={{ flexDirection: 'row', gap: 8, paddingTop: 10, paddingHorizontal: 16, paddingBottom: Math.max(insets.bottom, 12) + 16, borderTopWidth: 1, borderTopColor: t.line }}>
        <Field
          containerStyle={{ flex: 1 }}
          style={{ borderRadius: 99, minHeight: 50, flex: 1 }}
          value={draft}
          onChangeText={onDraft}
          onSubmitEditing={() => send(draft)}
          placeholder="Escreva uma mensagem"
          accessibilityLabel="Mensagem"
          returnKeyType="send"
          maxLength={500}
        />
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Enviar"
          onPress={() => send(draft)}
          style={{ width: 50, height: 50, borderRadius: 25, backgroundColor: t.accent, alignItems: 'center', justifyContent: 'center' }}
        >
          <Icon name="arrow" size={20} color="#FFFFFF" strokeWidth={2.2} />
        </Pressable>
      </View>
    </KeyboardAvoidingView>
  );
}

function Pill({ text }: { text: string }) {
  const { t } = useTheme();
  return (
    <View style={{ alignSelf: 'center', backgroundColor: t.s2, borderRadius: 99, paddingVertical: 6, paddingHorizontal: 12, marginBottom: 6, maxWidth: '90%' }}>
      <Text size={12} color={t.mut2} align="center">
        {text}
      </Text>
    </View>
  );
}

/** Balão: meu = azul à direita com o canto de baixo menor; do outro = s2 à esquerda. */
function Bubble({ mine, text }: { mine: boolean; text: string }) {
  const { t } = useTheme();
  return (
    <View
      style={{
        alignSelf: mine ? 'flex-end' : 'flex-start',
        maxWidth: '78%',
        paddingVertical: 10,
        paddingHorizontal: 14,
        borderRadius: 18,
        borderBottomRightRadius: mine ? 6 : 18,
        borderBottomLeftRadius: mine ? 18 : 6,
        backgroundColor: mine ? t.accent : t.s2,
      }}
    >
      <Text size={15} leading={1.4} color={mine ? '#FFFFFF' : t.fg}>
        {text}
      </Text>
    </View>
  );
}
