<script setup>
import QRCode from 'qrcode'
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import questions from '../questions.json'
import { useQuizSignalR } from '../useQuizSignalR'

const { connect, connected, error, invoke, on } = useQuizSignalR()
const phase = ref('settings')
const roomCode = ref('')
const players = ref([])
const questionIndex = ref(-1)
const answered = ref(0)
const questionDurationSeconds = ref(120)
const maxPlayers = ref(20)
const secondsRemaining = ref(questionDurationSeconds.value)
const nextQuestionCountdown = ref(0)
const isWaitingForNextQuestion = ref(false)
const settingsError = ref('')
const selectedQuestion = computed(() => questions[questionIndex.value])
const qrDataUrl = ref('')
const joinUrl = window.location.origin
const podium = computed(() => [...players.value].sort((a, b) => b.score - a.score).slice(0, 3))
const podiumSlots = computed(() => [1, 0, 2].map(index => ({ index, player: podium.value[index] })).filter(slot => slot.player))
const revealedPlaces = ref([])
let timerId
let nextQuestionIntervalId
let nextQuestionTimeoutId
let podiumRevealTimeoutId
let isAdvancing = false

onMounted(async () => {
  await connect()
  on('PlayerJoined', player => players.value.push(player))
  on('PlayerLeft', id => { players.value = players.value.filter(player => player.id !== id) })
  on('AnswersUpdated', progress => {
    answered.value = progress.answered
    if (phase.value === 'question' && progress.total > 0 && progress.answered >= progress.total) scheduleNextQuestion()
  })
  on('ScoresUpdated', scores => { players.value = scores })
})

async function createLobby() {
  settingsError.value = ''
  if (questionDurationSeconds.value < 10 || questionDurationSeconds.value > 600 || maxPlayers.value < 1 || maxPlayers.value > 100) {
    settingsError.value = 'Bitte wähle eine Zeit zwischen 10 und 600 Sekunden und 1 bis 100 Spieler.'
    return
  }
  try {
    const result = await invoke('CreateLobby', questionDurationSeconds.value, maxPlayers.value)
    roomCode.value = result.code
    qrDataUrl.value = await QRCode.toDataURL(`${window.location.origin}/?code=${result.code}`, { width: 280, margin: 1, color: { dark: '#152322', light: '#f7f5ef' } })
    phase.value = 'lobby'
  } catch (exception) {
    settingsError.value = exception.message || 'Der Quizraum konnte nicht erstellt werden.'
  }
}

function stopTimer() {
  if (timerId) {
    clearInterval(timerId)
    timerId = undefined
  }
}

function scheduleNextQuestion() {
  if (isAdvancing || isWaitingForNextQuestion.value) return
  stopTimer()
  isWaitingForNextQuestion.value = true
  nextQuestionCountdown.value = 5
  nextQuestionIntervalId = setInterval(() => { nextQuestionCountdown.value -= 1 }, 1000)
  nextQuestionTimeoutId = setTimeout(() => {
    clearInterval(nextQuestionIntervalId)
    nextQuestionIntervalId = undefined
    isWaitingForNextQuestion.value = false
    startQuestion()
  }, 5000)
}

function startTimer() {
  stopTimer()
  if (questionIndex.value === questions.length - 1) return
  secondsRemaining.value = questionDurationSeconds.value
  timerId = setInterval(() => {
    if (secondsRemaining.value <= 1) {
      secondsRemaining.value = 0
      stopTimer()
      startQuestion()
      return
    }
    secondsRemaining.value -= 1
  }, 1000)
}

function startPodiumReveal() {
  revealedPlaces.value = []
  const revealOrder = [2, 1, 0].filter(index => podium.value[index])
  let revealIndex = 0
  const revealNext = () => {
    if (revealIndex >= revealOrder.length) return
    revealedPlaces.value.push(revealOrder[revealIndex])
    revealIndex += 1
    if (revealIndex < revealOrder.length) podiumRevealTimeoutId = setTimeout(revealNext, 1500)
  }
  podiumRevealTimeoutId = setTimeout(revealNext, 700)
}

async function startQuestion() {
  if (isAdvancing || isWaitingForNextQuestion.value) return
  isAdvancing = true
  const nextIndex = questionIndex.value + 1
  if (nextIndex >= questions.length) {
    stopTimer()
    await invoke('EndQuiz', roomCode.value)
    phase.value = 'results'
    startPodiumReveal()
    isAdvancing = false
    return
  }
  questionIndex.value = nextIndex
  answered.value = 0
  phase.value = 'question'
  try {
    await invoke('StartNextQuestion', roomCode.value, nextIndex, questions[nextIndex].correctIndex)
    startTimer()
  } finally {
    isAdvancing = false
  }
}

onBeforeUnmount(() => {
  stopTimer()
  if (nextQuestionIntervalId) clearInterval(nextQuestionIntervalId)
  if (nextQuestionTimeoutId) clearTimeout(nextQuestionTimeoutId)
  if (podiumRevealTimeoutId) clearTimeout(podiumRevealTimeoutId)
})

function answerLabel(index) { return ['A', 'B', 'C', 'D'][index] }
function returnToStart() { window.location.href = window.location.pathname }
</script>

<template>
  <section class="admin-shell">
    <header class="admin-header">
      <div class="brand-mark"><span>FS</span> FAHRSICHER</div>
      <div class="connection-status" :class="{ offline: !connected }"><i></i>{{ connected ? 'LIVE VERBUNDEN' : 'VERBINDUNG...' }}</div>
    </header>
    <div v-if="phase === 'settings'" class="settings-layout">
      <div class="settings-panel">
        <p class="eyebrow">QUIZ EINRICHTEN</p>
        <h1>Deine<br /><em>Spielregeln.</em></h1>
        <label class="settings-field">Zeit pro Frage in Sekunden<input v-model.number="questionDurationSeconds" type="number" min="10" max="600" step="10" /></label>
        <label class="settings-field">Maximale Spielerzahl<input v-model.number="maxPlayers" type="number" min="1" max="100" /></label>
        <button class="primary-action" :disabled="!connected" @click="createLobby">Quizraum erstellen <span>→</span></button>
        <p v-if="settingsError || error" class="error-text">{{ settingsError || error }}</p>
      </div>
    </div>
    <div v-else-if="phase === 'lobby'" class="lobby-layout">
      <div class="lobby-hero">
        <p class="eyebrow">DEIN LIVE-QUIZ</p>
        <h1>Scannen.<br /><em>Einsteigen.</em></h1>
        <p class="subtle">Öffne die Kamera oder gehe zu<br /><strong>{{ joinUrl }}</strong></p>
        <button class="primary-action" :disabled="!connected" @click="startQuestion">Quiz beginnen <span>→</span></button>
      </div>
      <div class="code-card"><p class="eyebrow">RAUMCODE</p><strong>{{ roomCode }}</strong><img :src="qrDataUrl" alt="QR-Code zum Beitreten" /><p>Mit der Kamera scannen</p></div>
      <div class="player-roster"><div class="section-heading"><span>TEILNEHMER</span><b>{{ players.length }}</b></div><div v-if="!players.length" class="empty-roster">Noch wartet niemand.<br />Der Raum ist bereit.</div><div v-for="player in players" :key="player.id" class="player-row"><span>{{ player.name.charAt(0).toUpperCase() }}</span>{{ player.name }}</div></div>
    </div>
    <div v-else-if="phase === 'question'" class="question-layout" :class="{ switching: isWaitingForNextQuestion }">
      <div v-if="isWaitingForNextQuestion" class="question-transition"><strong>Nächste Frage</strong><span>Startet in {{ nextQuestionCountdown }} Sekunden</span></div>
      <div class="question-meta"><span>FRAGE {{ questionIndex + 1 }} / {{ questions.length }}</span><b v-if="questionIndex < questions.length - 1">{{ Math.floor(secondsRemaining / 60) }}:{{ String(secondsRemaining % 60).padStart(2, '0') }} MIN · {{ answered }} / {{ players.length }} ANTWORTEN</b><b v-else>{{ answered }} / {{ players.length }} ANTWORTEN</b></div>
      <h1>{{ selectedQuestion.question }}</h1>
      <video v-if="selectedQuestion.mediaUrl" :src="selectedQuestion.mediaUrl" controls />
      <div class="admin-options"><div v-for="(option, index) in selectedQuestion.options" :key="option" class="admin-option" :class="`option-${index}`"><b>{{ answerLabel(index) }}</b><span>{{ option }}</span></div></div>
      <button class="primary-action next-button" @click="startQuestion">{{ questionIndex + 1 < questions.length ? 'Nächste Frage' : 'Ergebnisse anzeigen' }} <span>→</span></button>
    </div>
    <div v-else class="results-layout"><p class="eyebrow">QUIZ BEENDET</p><h1>Das Siegerpodest<span>.</span></h1><div class="winner-podium"><div v-for="slot in podiumSlots" :key="slot.player.id" class="winner-slot" :class="[`rank-${slot.index + 1}`, { revealed: revealedPlaces.includes(slot.index) }]" ><span class="winner-name">{{ slot.player.name }}</span><strong>{{ slot.player.score }} P</strong><b>{{ slot.index + 1 }}</b></div></div><button class="primary-action" @click="returnToStart">Zur Startseite <span>→</span></button></div>
    <p v-if="error" class="error-text admin-error">{{ error }}</p>
  </section>
</template>
