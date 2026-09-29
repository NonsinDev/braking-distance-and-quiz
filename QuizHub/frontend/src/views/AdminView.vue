<script setup>
import QRCode from 'qrcode'
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useQuizSignalR } from '../useQuizSignalR'

const { connect, connected, error, invoke, on } = useQuizSignalR()
const phase = ref('settings')
const roomCode = ref('')
const quizTitle = ref('')
const players = ref([])
const availableQuizzes = ref([])
const selectedQuizId = ref('')

const questionIndex = ref(-1)
const totalQuestions = ref(0)
const currentQuestion = ref(null)
const revealedCorrectIndices = ref([])

const answered = ref(0)
const questionDurationSeconds = ref(20)
const maxPlayers = ref(20)
const secondsRemaining = ref(20)
const nextQuestionCountdown = ref(0)
const isWaitingForNextQuestion = ref(false)
const settingsError = ref('')

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

async function loadQuizzes() {
  try {
    const list = await invoke('GetAvailableQuizzes')
    if (Array.isArray(list) && list.length > 0) {
      availableQuizzes.value = list
      if (!selectedQuizId.value) {
        selectedQuizId.value = list[0].id
      }
    }
  } catch (err) {
    console.warn('Konnte Quizzes nicht abrufen:', err)
  }
}

onMounted(async () => {
  await connect()
  await loadQuizzes()

  on('PlayerJoined', player => {
    if (!players.value.some(p => p.id === player.id)) {
      players.value.push(player)
    }
  })

  on('PlayerLeft', id => {
    players.value = players.value.filter(player => player.id !== id)
  })

  on('QuestionStarted', data => {
    applyQuestionStarted(data)
  })

  on('QuestionEnded', ({ questionIndex: qIdx, correctIndices }) => {
    revealedCorrectIndices.value = correctIndices || []
  })

  on('AnswersUpdated', progress => {
    answered.value = progress.answered
    if (phase.value === 'question' && progress.total > 0 && progress.answered >= progress.total) {
      revealAndScheduleNext()
    }
  })

  on('ScoresUpdated', scores => {
    players.value = scores
  })
})

function applyQuestionStarted(data) {
  if (!data) return
  if (phase.value === 'question' && questionIndex.value === data.questionIndex) {
    return
  }
  currentQuestion.value = data
  questionIndex.value = data.questionIndex
  totalQuestions.value = data.totalQuestions
  secondsRemaining.value = data.timeLimitSeconds
  revealedCorrectIndices.value = []
  answered.value = 0
  phase.value = 'question'
  isWaitingForNextQuestion.value = false
  startTimer()
}

async function createLobby() {
  settingsError.value = ''
  if (questionDurationSeconds.value < 10 || questionDurationSeconds.value > 600 || maxPlayers.value < 1 || maxPlayers.value > 100) {
    settingsError.value = 'Bitte wähle eine Zeit zwischen 10 und 600 Sekunden und 1 bis 100 Spieler.'
    return
  }
  if (!selectedQuizId.value && availableQuizzes.value.length > 0) {
    selectedQuizId.value = availableQuizzes.value[0].id
  }

  try {
    const result = await invoke('CreateLobby', selectedQuizId.value, questionDurationSeconds.value, maxPlayers.value)
    roomCode.value = result.code
    quizTitle.value = result.quizTitle || 'QuizHub'
    totalQuestions.value = result.totalQuestions || 0
    qrDataUrl.value = await QRCode.toDataURL(`${window.location.origin}/?code=${result.code}`, {
      width: 280,
      margin: 1,
      color: { dark: '#152322', light: '#f7f5ef' }
    })
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

async function revealAnswers() {
  if (revealedCorrectIndices.value.length > 0) return
  try {
    const res = await invoke('RevealQuestion', roomCode.value)
    if (res?.correctIndices) {
      revealedCorrectIndices.value = res.correctIndices
    }
  } catch (err) {
    console.error('Fehler beim Auflösen:', err)
  }
}

function revealAndScheduleNext() {
  if (isAdvancing || isWaitingForNextQuestion.value) return
  stopTimer()
  revealAnswers()

  // Nach der letzten Frage keinen Übergangs-Ladebalken ("Nächste Frage") mehr anzeigen
  const isLastQuestion = totalQuestions.value > 0 && questionIndex.value >= totalQuestions.value - 1
  if (isLastQuestion) {
    return
  }

  isWaitingForNextQuestion.value = true
  nextQuestionCountdown.value = 4
  nextQuestionIntervalId = setInterval(() => { nextQuestionCountdown.value -= 1 }, 1000)
  nextQuestionTimeoutId = setTimeout(() => {
    clearInterval(nextQuestionIntervalId)
    nextQuestionIntervalId = undefined
    isWaitingForNextQuestion.value = false
    startQuestion()
  }, 4000)
}

function startTimer() {
  stopTimer()
  timerId = setInterval(() => {
    if (secondsRemaining.value <= 1) {
      secondsRemaining.value = 0
      stopTimer()
      revealAnswers()
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
  if (totalQuestions.value > 0 && nextIndex >= totalQuestions.value) {
    stopTimer()
    await invoke('EndQuiz', roomCode.value)
    phase.value = 'results'
    startPodiumReveal()
    isAdvancing = false
    return
  }
  try {
    const data = await invoke('StartNextQuestion', roomCode.value, nextIndex)
    if (data) {
      applyQuestionStarted(data)
    }
  } catch (err) {
    console.error('Fehler beim Starten der nächsten Frage:', err)
    error.value = err.message || 'Fehler beim Starten der nächsten Frage.'
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

function answerLabel(index) { return ['A', 'B', 'C', 'D', 'E', 'F'][index] || `${index + 1}` }
function returnToStart() { window.location.href = window.location.pathname }
</script>

<template>
  <section class="admin-shell">
    <header class="admin-header">
      <div class="brand-mark"><span>QH</span> QUIZHUB</div>
      <div class="connection-status" :class="{ offline: !connected }">
        <i></i>{{ connected ? 'LIVE VERBUNDEN' : 'VERBINDUNG...' }}
      </div>
    </header>

    <!-- Phase 1: Settings / Quiz Auswahl -->
    <div v-if="phase === 'settings'" class="settings-layout">
      <div class="settings-panel">
        <p class="eyebrow">QUIZ EINRICHTEN</p>
        <h1>Deine<br /><em>Spielregeln.</em></h1>

        <div class="settings-field">
          <span>Quiz auswählen</span>
          <div v-if="availableQuizzes.length" class="quiz-selector">
            <div
              v-for="quiz in availableQuizzes"
              :key="quiz.id"
              class="quiz-card"
              :class="{ active: selectedQuizId === quiz.id }"
              @click="selectedQuizId = quiz.id"
            >
              <strong>{{ quiz.name }}</strong>
              <p v-if="quiz.description">{{ quiz.description }}</p>
              <span class="quiz-badge">{{ quiz.questionCount }} Fragen</span>
            </div>
          </div>
          <p v-else class="subtle" style="margin-top: 10px;">Lade Quizzes aus dem Quiz-Ordner...</p>
        </div>

        <label class="settings-field">
          Standard-Zeit pro Frage (Sekunden)
          <input v-model.number="questionDurationSeconds" type="number" min="10" max="600" step="5" />
        </label>

        <label class="settings-field">
          Maximale Spielerzahl
          <input v-model.number="maxPlayers" type="number" min="1" max="100" />
        </label>

        <button class="primary-action" :disabled="!connected" @click="createLobby">
          Quizraum erstellen <span>→</span>
        </button>
        <p v-if="settingsError || error" class="error-text">{{ settingsError || error }}</p>
      </div>
    </div>

    <!-- Phase 2: Lobby / Wartesaal -->
    <div v-else-if="phase === 'lobby'" class="lobby-layout">
      <div class="lobby-hero">
        <p class="eyebrow">QUIZ: {{ quizTitle }}</p>
        <h1>Scannen.<br /><em>Einsteigen.</em></h1>
        <p class="subtle">Öffne die Kamera oder gehe zu<br /><strong>{{ joinUrl }}</strong></p>
        <button class="primary-action" :disabled="!connected" @click="startQuestion">
          Quiz beginnen <span>→</span>
        </button>
      </div>
      <div class="code-card">
        <p class="eyebrow">RAUMCODE</p>
        <strong>{{ roomCode }}</strong>
        <img :src="qrDataUrl" alt="QR-Code zum Beitreten" />
        <p>Mit der Kamera scannen</p>
      </div>
      <div class="player-roster">
        <div class="section-heading">
          <span>TEILNEHMER</span>
          <b>{{ players.length }}</b>
        </div>
        <div v-if="!players.length" class="empty-roster">
          Noch wartet niemand.<br />Der Raum ist bereit.
        </div>
        <div v-for="player in players" :key="player.id" class="player-row">
          <span>{{ player.name.charAt(0).toUpperCase() }}</span>
          {{ player.name }}
        </div>
      </div>
    </div>

    <!-- Phase 3: Question Display -->
    <div v-else-if="phase === 'question' && currentQuestion" class="question-layout">
      <div v-if="isWaitingForNextQuestion" class="question-transition-overlay">
        <div class="question-transition-box">
          <p class="eyebrow">BEREIT MACHEN</p>
          <strong>Nächste Frage</strong>
          <span>Startet in {{ nextQuestionCountdown }} Sekunden</span>
          <div class="transition-progress">
            <div class="transition-bar"></div>
          </div>
        </div>
      </div>

      <div class="question-meta">
        <span>FRAGE {{ questionIndex + 1 }} / {{ totalQuestions }}</span>
        <b>{{ Math.floor(secondsRemaining / 60) }}:{{ String(secondsRemaining % 60).padStart(2, '0') }} MIN · {{ answered }} / {{ players.length }} ANTWORTEN</b>
      </div>

      <!-- Multiple answers notification banner -->
      <div v-if="currentQuestion.hasMultipleCorrectAnswers" class="multi-answer-banner">
        <span class="multi-tag">MEHRFACHAUSWAHL</span>
        <span>Mehrere Antworten sind richtig!</span>
      </div>

      <h1>{{ currentQuestion.question }}</h1>
      <video v-if="currentQuestion.mediaUrl" :src="currentQuestion.mediaUrl" controls />

      <div class="admin-options">
        <div
          v-for="(option, index) in currentQuestion.options"
          :key="option"
          class="admin-option"
          :class="[
            `option-${index % 4}`,
            {
              'is-correct': revealedCorrectIndices.includes(index),
              'is-wrong': revealedCorrectIndices.length > 0 && !revealedCorrectIndices.includes(index)
            }
          ]"
        >
          <b>{{ answerLabel(index) }}</b>
          <span>{{ option }}</span>
          <span v-if="revealedCorrectIndices.includes(index)" class="status-tag correct">Richtig</span>
        </div>
      </div>

      <div class="admin-controls">
        <button class="primary-action next-button" @click="startQuestion">
          {{ questionIndex + 1 < totalQuestions ? 'Nächste Frage' : 'Ergebnisse anzeigen' }} <span>→</span>
        </button>
      </div>
    </div>

    <!-- Phase 4: Results Podium -->
    <div v-else class="results-layout">
      <p class="eyebrow">QUIZ BEENDET</p>
      <h1>Das Siegerpodest<span>.</span></h1>
      <div class="winner-podium">
        <div
          v-for="slot in podiumSlots"
          :key="slot.player.id"
          class="winner-slot"
          :class="[`rank-${slot.index + 1}`, { revealed: revealedPlaces.includes(slot.index) }]"
        >
          <span class="winner-name">{{ slot.player.name }}</span>
          <strong>{{ slot.player.score }} P</strong>
          <b>{{ slot.index + 1 }}</b>
        </div>
      </div>
      <button class="primary-action" @click="returnToStart">Zur Startseite <span>→</span></button>
    </div>

    <p v-if="error" class="error-text admin-error">{{ error }}</p>
  </section>
</template>
